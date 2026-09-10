using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using G5.Logic;

namespace G5.Chipzen
{
    class Program
    {
        static async Task<int> Main()
        {
            string? wsUrl = Environment.GetEnvironmentVariable("CHIPZEN_WS_URL")
                ?? Environment.GetEnvironmentVariable("CHIPZEN_URL");
            string? token = Environment.GetEnvironmentVariable("CHIPZEN_TOKEN");
            string? ticket = Environment.GetEnvironmentVariable("CHIPZEN_TICKET");

            if (string.IsNullOrEmpty(wsUrl))
            {
                Console.Error.WriteLine("CHIPZEN_WS_URL environment variable is required.");
                return 2;
            }

            if (string.IsNullOrEmpty(token) && string.IsNullOrEmpty(ticket))
            {
                // Neither is a hard requirement: the sandboxed/uploaded executor may authenticate
                // the container by network position alone (TRANSPORT-PROTOCOL.md §4.5's
                // "network-level isolation" alternative to a token), in which case there is
                // nothing to read here. Warn and proceed rather than refuse to even attempt a
                // connection -- let the server's own authenticate response be the source of truth.
                Console.WriteLine("WARNING: neither CHIPZEN_TOKEN nor CHIPZEN_TICKET is set; " +
                    "connecting without explicit credentials.");
            }

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            // Kick off every slow, match-independent load in the background at process start,
            // overlapping all of it with the WebSocket connect/handshake below rather than
            // leaving any of it to happen lazily (and synchronously) on the first round_start --
            // see the "lazy loading on first hand" investigation for why that was timing out the
            // bot's first action.
            var opponentModelingTask = Task.Run(() =>
            {
                var options = new OpponentModeling.Options { recentHandsCount = 1000 };
                return new OpponentModeling("full_stats_list_hu.bin", TableType.HeadsUp, options);
            });

            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var preFlopChartsTask = Task.Run(() =>
                new PreFlopCharts(Path.Combine(assemblyFolder, "PreFlopCharts", "200bb")));

            // Also forces the native DecisionMaking.dll/libdec_making.so to be loaded/linked now
            // (the first P/Invoke call into it is what actually maps the library into the
            // process), instead of on the first hand.
            var dmContextTask = Task.Run(() => new DecisionMakingContext());

            using var client = new ChipzenClient(wsUrl, token, ticket, clientName: "g5-chipzen", clientVersion: "0.1.0");

            try
            {
                await client.ConnectAsync(cts.Token);
                Console.WriteLine($"Connected. Match id: {client.MatchId}");

                using var game = new ChipzenGame(opponentModelingTask, preFlopChartsTask, dmContextTask);
                await client.RunAsync(game, cts.Token);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Fatal error: " + ex);
                return 1;
            }

            Console.WriteLine("Bot exiting cleanly.");
            return 0;
        }
    }
}
