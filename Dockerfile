# Builds the G5 poker bot for the Chipzen platform (heads-up NLHE, package-and-upload path).
# Two stages purely to keep the shipped image small -- this project is open source, so unlike
# Chipzen's Cython/Rust starter patterns there is no attempt to strip source from the final image.
#
# The runtime stage uses a "chiseled" (distroless-style) base to fit the 200MB image-size cap --
# self-contained .NET publish + native TBB-linked lib + data files don't fit comfortably in a
# full Debian/Ubuntu base. The builder stage uses plain ubuntu:22.04 (jammy) rather than the
# Debian-bookworm-based dotnet/sdk image so DecisionMaking.so and libtbb.so are built/sourced
# against the same glibc as the jammy-chiseled runtime they'll run on.

FROM ubuntu:22.04 AS builder

RUN apt-get update \
    && apt-get install -y --no-install-recommends build-essential libtbb-dev curl ca-certificates libicu70 \
    && rm -rf /var/lib/apt/lists/*

RUN curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && rm /tmp/dotnet-install.sh

WORKDIR /src
COPY src/DecisionMaking/ ./DecisionMaking/
COPY src/G5.Logic/ ./G5.Logic/
COPY src/G5.Chipzen/ ./G5.Chipzen/

RUN cd DecisionMaking && make && cd .. \
    && dotnet publish G5.Chipzen -c Release -f net8.0 -r linux-x64 --self-contained true -o /out

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled AS runtime

WORKDIR /bot
COPY --from=builder /out/ ./
COPY --from=builder /src/DecisionMaking/libdec_making.so ./DecisionMaking.dll
COPY --from=builder /usr/lib/x86_64-linux-gnu/libtbb.so.12 /usr/lib/x86_64-linux-gnu/
COPY redist/PreFlopEquities.txt ./
COPY redist/full_stats_list_hu.bin ./
COPY redist/PreFlopCharts/200bb/ ./PreFlopCharts/200bb/

# The chiseled base already runs as a non-root user by default -- no USER/groupadd needed
# (and unavailable anyway: chiseled images ship no shell or package manager).

ENTRYPOINT ["./G5.Chipzen"]
