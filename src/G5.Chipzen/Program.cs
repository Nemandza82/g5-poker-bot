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
                Console.Error.WriteLine("Either CHIPZEN_TOKEN or CHIPZEN_TICKET environment variable is required.");
                return 2;
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
