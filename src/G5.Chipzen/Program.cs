using System;
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

            // Kick off the (relatively slow) opponent-model stats-file load in the background,
            // overlapping it with the WebSocket connect/handshake below rather than adding it to
            // the container's attach-time budget.
            var opponentModelingTask = Task.Run(() =>
            {
                var options = new OpponentModeling.Options { recentHandsCount = 1000 };
                return new OpponentModeling("full_stats_list_hu.bin", TableType.HeadsUp, options);
            });

            using var client = new ChipzenClient(wsUrl, token, ticket, clientName: "g5-chipzen", clientVersion: "0.1.0");

            try
            {
                await client.ConnectAsync(cts.Token);
                Console.WriteLine($"Connected. Match id: {client.MatchId}");

                using var game = new ChipzenGame(opponentModelingTask);
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
