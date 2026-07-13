// Local test tool: a minimal, adaptive fake Chipzen match server used to exercise the real
// G5.Chipzen container end-to-end (real WebSocket, real handshake, real hand play) without
// needing access to the actual Chipzen platform. Not part of the shipped bot image.
//
// Usage:
//   dotnet run --project tools/mock-chipzen-server
// then point a running g5-chipzen container at the printed CHIPZEN_WS_URL (via
// host.docker.internal so the container can reach this process on the host), e.g.:
//   docker run --rm --user 10001:10001 --add-host=host.docker.internal:host-gateway \
//     -e CHIPZEN_WS_URL=ws://host.docker.internal:8765/ws/match/<id>/p_bot -e CHIPZEN_TOKEN=dummy \
//     g5-chipzen:dev
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

const int OPP = 0, BOT = 1;
const int StartingStack = 1000, SmallBlind = 5, BigBlind = 10;
const string WebSocketGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

var matchId = Guid.NewGuid().ToString();

// Plain TcpListener + a hand-rolled RFC6455 upgrade, bound to all interfaces -- unlike
// HttpListener's "+"/wildcard host binding, this needs no admin rights / URL ACL reservation,
// which matters here since the real client is a Docker container connecting in via
// host.docker.internal rather than "localhost".
var tcpListener = new TcpListener(IPAddress.Any, 8765);
tcpListener.Start();

Console.WriteLine("Mock Chipzen server listening on tcp://0.0.0.0:8765/ws/");
Console.WriteLine($"Point the bot at: CHIPZEN_WS_URL=ws://host.docker.internal:8765/ws/match/{matchId}/p_bot  CHIPZEN_TOKEN=dummy");

using var tcpClient = await tcpListener.AcceptTcpClientAsync();
var networkStream = tcpClient.GetStream();

string requestText;
{
    var buffer = new byte[8192];
    using var ms = new MemoryStream();
    while (true)
    {
        int n = await networkStream.ReadAsync(buffer);
        ms.Write(buffer, 0, n);
        var soFar = Encoding.ASCII.GetString(ms.ToArray());
        if (soFar.Contains("\r\n\r\n")) { requestText = soFar; break; }
    }
}

string? secWebSocketKey = requestText
    .Split("\r\n")
    .FirstOrDefault(l => l.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
    ?.Split(':', 2)[1].Trim();

if (secWebSocketKey is null)
{
    Console.WriteLine("Not a WebSocket upgrade request:\n" + requestText);
    return;
}

string accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(secWebSocketKey + WebSocketGuid)));
string response =
    "HTTP/1.1 101 Switching Protocols\r\n" +
    "Upgrade: websocket\r\n" +
    "Connection: Upgrade\r\n" +
    $"Sec-WebSocket-Accept: {accept}\r\n\r\n";
await networkStream.WriteAsync(Encoding.ASCII.GetBytes(response));

var ws = WebSocket.CreateFromStream(networkStream, isServer: true, subProtocol: null, keepAliveInterval: TimeSpan.FromSeconds(30));
Console.WriteLine("Bot connected.");

int seq = 0;
int NextSeq() => ++seq;
string Ts() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

async Task SendAsync(JsonObject msg)
{
    var bytes = Encoding.UTF8.GetBytes(msg.ToJsonString());
    await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    Console.WriteLine("S>B " + msg.ToJsonString());
}

async Task<JsonObject> RecvAsync()
{
    var buffer = new byte[16384];
    using var stream = new MemoryStream();
    while (true)
    {
        var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
        stream.Write(buffer, 0, result.Count);
        if (result.EndOfMessage)
        {
            var text = Encoding.UTF8.GetString(stream.ToArray());
            Console.WriteLine("B>S " + text);
            return (JsonObject)JsonNode.Parse(text)!;
        }
    }
}

JsonArray CloneArray(JsonArray arr) => (JsonArray)JsonNode.Parse(arr.ToJsonString())!;

// ---- Handshake ----
await RecvAsync(); // authenticate
await SendAsync(new JsonObject
{
    ["type"] = "hello", ["match_id"] = matchId, ["seq"] = NextSeq(), ["server_ts"] = Ts(),
    ["supported_versions"] = new JsonArray("1.0"), ["selected_version"] = "1.0",
    ["game_type"] = "nlhe_heads_up", ["capabilities"] = new JsonArray(),
});
await RecvAsync(); // client hello

await SendAsync(new JsonObject
{
    ["type"] = "match_start", ["match_id"] = matchId, ["seq"] = NextSeq(), ["server_ts"] = Ts(),
    ["seats"] = new JsonArray(
        new JsonObject { ["seat"] = OPP, ["participant_id"] = "p_opp", ["display_name"] = "Opponent", ["is_self"] = false },
        new JsonObject { ["seat"] = BOT, ["participant_id"] = "p_bot", ["display_name"] = "G5", ["is_self"] = true }),
    ["game_config"] = new JsonObject
    {
        ["variant"] = "nlhe", ["starting_stack"] = StartingStack, ["small_blind"] = SmallBlind,
        ["big_blind"] = BigBlind, ["ante"] = 0, ["total_hands"] = 0,
    },
    ["turn_timeout_ms"] = 5000,
});

// ---- Play a couple of hands, alternating the button, to exercise both seating roles ----
int[] stacks = { StartingStack, StartingStack };
string[][] holeCardsByHand = { new[] { "Ah", "Kd" }, new[] { "7c", "7d" } };
string[] board = { "Ts", "7h", "2c", "Qd", "9s" };

for (int handNumber = 1; handNumber <= 2 && stacks[OPP] > 0 && stacks[BOT] > 0; handNumber++)
{
    int dealerSeat = (handNumber % 2 == 1) ? OPP : BOT;
    stacks = await PlayHandAsync(handNumber, dealerSeat, stacks, holeCardsByHand[handNumber - 1]);
}

await SendAsync(new JsonObject
{
    ["type"] = "match_end", ["match_id"] = matchId, ["seq"] = NextSeq(), ["server_ts"] = Ts(),
    ["reason"] = "complete",
    ["results"] = new JsonArray(
        new JsonObject { ["seat"] = OPP, ["participant_id"] = "p_opp", ["rank"] = stacks[OPP] >= stacks[BOT] ? 1 : 2, ["score"] = stacks[OPP] },
        new JsonObject { ["seat"] = BOT, ["participant_id"] = "p_bot", ["rank"] = stacks[BOT] > stacks[OPP] ? 1 : 2, ["score"] = stacks[BOT] }),
});

Console.WriteLine($"Match complete. Final stacks: opponent={stacks[OPP]}, bot={stacks[BOT]}");
ws.Dispose();
tcpListener.Stop();
return;

// ---- Plays one full heads-up hand, adapting to whatever the bot actually decides. ----
// The scripted "opponent" always checks/calls (matching the bot's bet), never raises or folds --
// simple, deterministic, and sufficient to walk the bot through every street to showdown.
async Task<int[]> PlayHandAsync(int handNumber, int dealerSeat, int[] startStacks, string[] botHoleCards)
{
    var s = (int[])startStacks.Clone();
    var actionHistory = new JsonArray();
    var streetContribution = new int[2];
    int pot = 0;

    void Record(int seat, string action, int amount, string phase) =>
        actionHistory.Add(new JsonObject { ["seat"] = seat, ["action"] = action, ["amount"] = amount, ["phase"] = phase, ["is_timeout"] = false });

    await SendAsync(new JsonObject
    {
        ["type"] = "round_start", ["match_id"] = matchId, ["seq"] = NextSeq(), ["server_ts"] = Ts(),
        ["round_id"] = Guid.NewGuid().ToString(), ["round_number"] = handNumber,
        ["state"] = new JsonObject
        {
            ["hand_number"] = handNumber, ["dealer_seat"] = dealerSeat,
            ["your_hole_cards"] = new JsonArray(botHoleCards[0], botHoleCards[1]),
            ["stacks"] = new JsonArray(s[0], s[1]),
            ["deck_commitment"] = "",
        },
    });

    // Blinds: dealer posts SB and acts first preflop (heads-up rule).
    s[dealerSeat] -= SmallBlind;
    s[1 - dealerSeat] -= BigBlind;
    streetContribution[dealerSeat] = SmallBlind;
    streetContribution[1 - dealerSeat] = BigBlind;
    pot = SmallBlind + BigBlind;
    Record(dealerSeat, "post_small_blind", SmallBlind, "preflop");
    Record(1 - dealerSeat, "post_big_blind", BigBlind, "preflop");

    string[] phases = { "preflop", "flop", "turn", "river" };
    bool botFolded = false;

    for (int phaseIdx = 0; phaseIdx < phases.Length && !botFolded; phaseIdx++)
    {
        string phase = phases[phaseIdx];
        int firstToAct = (phase == "preflop") ? dealerSeat : 1 - dealerSeat;
        int actorSeat = firstToAct;
        int actsThisStreet = 0;

        while (true)
        {
            int toCall = Math.Max(streetContribution[1 - actorSeat] - streetContribution[actorSeat], 0);

            if (actorSeat == OPP)
            {
                string action = toCall == 0 ? "check" : "call";
                int amount = toCall;
                s[OPP] -= amount;
                streetContribution[OPP] += amount;
                pot += amount;
                Record(OPP, action, amount, phase);
                await SendAsync(new JsonObject
                {
                    ["type"] = "turn_result", ["match_id"] = matchId, ["seq"] = NextSeq(), ["server_ts"] = Ts(),
                    ["seat"] = OPP, ["details"] = new JsonObject { ["seat"] = OPP, ["action"] = action, ["amount"] = amount },
                });
            }
            else
            {
                int minRaise = streetContribution[BOT] + toCall + BigBlind;
                int maxRaise = s[BOT] + streetContribution[BOT];
                var validActions = new JsonArray();
                if (toCall == 0) validActions.Add("check"); else { validActions.Add("fold"); validActions.Add("call"); }
                if (s[BOT] > toCall && minRaise < maxRaise) validActions.Add("raise");
                validActions.Add("all_in");

                int cardsShown = phaseIdx == 0 ? 0 : phaseIdx + 2;
                var requestId = "req_" + Guid.NewGuid().ToString("N")[..8];

                await SendAsync(new JsonObject
                {
                    ["type"] = "turn_request", ["match_id"] = matchId, ["seq"] = NextSeq(), ["server_ts"] = Ts(),
                    ["seat"] = BOT, ["request_id"] = requestId, ["timeout_ms"] = 5000,
                    ["valid_actions"] = validActions,
                    ["state"] = new JsonObject
                    {
                        ["hand_number"] = handNumber, ["phase"] = phase,
                        ["board"] = new JsonArray(board.Take(cardsShown).Select(c => (JsonNode)c).ToArray()),
                        ["your_hole_cards"] = new JsonArray(botHoleCards[0], botHoleCards[1]),
                        ["pot"] = pot, ["your_stack"] = s[BOT], ["opponent_stacks"] = new JsonArray(s[OPP]),
                        ["to_call"] = toCall, ["min_raise"] = minRaise, ["max_raise"] = maxRaise,
                        ["action_history"] = CloneArray(actionHistory),
                    },
                });

                var turnAction = await RecvAsync();
                string action = (string)turnAction["action"]!;
                int amount = 0;

                if (action == "fold")
                {
                    Record(BOT, "fold", 0, phase);
                    await SendAsync(new JsonObject
                    {
                        ["type"] = "turn_result", ["match_id"] = matchId, ["seq"] = NextSeq(), ["server_ts"] = Ts(),
                        ["seat"] = BOT, ["details"] = new JsonObject { ["seat"] = BOT, ["action"] = "fold", ["amount"] = 0 },
                    });
                    botFolded = true;
                    break;
                }

                if (action == "raise" || action == "all_in")
                {
                    amount = action == "all_in" ? maxRaise : (int)turnAction["params"]!["amount"]!;
                    int delta = amount - streetContribution[BOT];
                    s[BOT] -= delta;
                    streetContribution[BOT] = amount;
                    pot += delta;
                    Record(BOT, action, amount, phase);
                }
                else if (action == "call")
                {
                    amount = toCall;
                    s[BOT] -= amount;
                    streetContribution[BOT] += amount;
                    pot += amount;
                    Record(BOT, "call", amount, phase);
                }
                else
                {
                    Record(BOT, "check", 0, phase);
                }

                await SendAsync(new JsonObject
                {
                    ["type"] = "turn_result", ["match_id"] = matchId, ["seq"] = NextSeq(), ["server_ts"] = Ts(),
                    ["seat"] = BOT, ["details"] = new JsonObject { ["seat"] = BOT, ["action"] = action, ["amount"] = amount },
                });

                if (action == "raise" || action == "all_in")
                {
                    int oppCost = Math.Min(streetContribution[BOT] - streetContribution[OPP], s[OPP]);
                    s[OPP] -= oppCost;
                    streetContribution[OPP] += oppCost;
                    pot += oppCost;
                    Record(OPP, "call", oppCost, phase);
                    await SendAsync(new JsonObject
                    {
                        ["type"] = "turn_result", ["match_id"] = matchId, ["seq"] = NextSeq(), ["server_ts"] = Ts(),
                        ["seat"] = OPP, ["details"] = new JsonObject { ["seat"] = OPP, ["action"] = "call", ["amount"] = oppCost },
                    });
                    // The scripted opponent never re-raises, so a call closing out a bot raise
                    // always ends the street here -- don't loop back around for another (bogus)
                    // opponent turn.
                    break;
                }
            }

            actsThisStreet++;
            if (streetContribution[OPP] == streetContribution[BOT] && actsThisStreet >= 2)
                break;
            actorSeat = 1 - actorSeat;
        }

        if (botFolded)
            break;

        streetContribution[0] = 0;
        streetContribution[1] = 0;

        if (phaseIdx < phases.Length - 1)
        {
            int cardsAfter = phaseIdx + 3;
            await SendAsync(new JsonObject
            {
                ["type"] = "phase_change", ["match_id"] = matchId, ["seq"] = NextSeq(), ["server_ts"] = Ts(),
                ["state"] = new JsonObject { ["phase"] = phases[phaseIdx + 1], ["board"] = new JsonArray(board.Take(cardsAfter).Select(c => (JsonNode)c).ToArray()) },
            });
        }
    }

    int winnerSeat = botFolded ? OPP : BOT;
    if (winnerSeat == BOT) s[BOT] += pot; else s[OPP] += pot;

    await SendAsync(new JsonObject
    {
        ["type"] = "round_result", ["match_id"] = matchId, ["seq"] = NextSeq(), ["server_ts"] = Ts(),
        ["round_id"] = Guid.NewGuid().ToString(), ["round_number"] = handNumber,
        ["result"] = new JsonObject
        {
            ["hand_number"] = handNumber, ["winner_seats"] = new JsonArray(winnerSeat), ["pot"] = pot,
            ["payouts"] = new JsonArray(new JsonObject { ["seat"] = winnerSeat, ["amount"] = pot }),
            ["showdown"] = botFolded ? new JsonArray() : new JsonArray(
                new JsonObject { ["seat"] = BOT, ["hole_cards"] = new JsonArray(botHoleCards[0], botHoleCards[1]), ["hand_rank"] = "pair" },
                new JsonObject { ["seat"] = OPP, ["hole_cards"] = new JsonArray("2c", "3d"), ["hand_rank"] = "high_card" }),
            ["action_history"] = CloneArray(actionHistory),
            ["stacks"] = new JsonArray(s[OPP], s[BOT]),
            ["deck_commitment"] = "",
            ["deck_reveal"] = null,
        },
    });

    Console.WriteLine($"--- Hand {handNumber} done. winner=seat{winnerSeat} pot={pot} stacks=[{s[OPP]},{s[BOT]}] ---");
    return s;
}
