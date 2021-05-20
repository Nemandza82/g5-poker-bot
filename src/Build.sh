#!/bin/bash

# Build DecisionMaking dll
cd DecisionMaking
make
cd ..

# Clean contents of bin dir
mkdir bin
cd bin
rm *
cd ..

# Build the dotnet projects...
cd G5.Acpc
dotnet restore
dotnet publish -c Release -o ../bin -f netcoreapp1.1 -r ubuntu.16.04-x64
cd ..

# Copy all redist files (probabilities etc)
cp DecisionMaking/libdec_making.so bin/DecisionMaking.dll
cp ../redist/PreFlopEquities.txt bin/
cp ../redist/full_stats_list_6max.bin bin/
cp ../redist/full_stats_list_6max_2m.bin bin/
cp ../redist/full_stats_list_hu.bin bin/

cp G5.Acpc/README bin/
cp G5.Acpc/startme.sh bin/
