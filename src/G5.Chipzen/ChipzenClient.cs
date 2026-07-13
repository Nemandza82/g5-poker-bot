using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace G5.Chipzen
{
    /// <summary>
    /// Layer 1 (transport) client for the Chipzen match protocol, as specified in
    /// thirdparty/chipzen-sdk/docs/protocol/TRANSPORT-PROTOCOL.md. Game-agnostic: connects,
    /// authenticates, handshakes, and dispatches Layer 2 (poker) payloads to an IChipzenGame.
    /// </summary>
    public sealed class ChipzenClient : IDisposable
    {
        private const int ReceiveBufferSize = 16 * 1024;

        private readonly Uri _url;
        private readonly string? _token;
        private readonly string? _ticket;
        private readonly string _clientName;
        private readonly string _clientVersion;
        private readonly ClientWebSocket _ws = new();

        public string MatchId { get; private set; } = "";

        public ChipzenClient(string url, string? token, string? ticket,
            string clientName = "g5-chipzen", string clientVersion = "0.1.0")
        {
            _url = new Uri(url);
            _token = token;
            _ticket = ticket;
            _clientName = clientName;
            _clientVersion = clientVersion;
            MatchId = ExtractMatchIdFromUrl(_url);
        }

        private static string ExtractMatchIdFromUrl(Uri url)
        {
            // Path shape: /ws/match/{match_id}/{participant_id} (or /ws/reconnect/{match_id}/{participant_id}).
            var segments = url.AbsolutePath.Trim('/').Split('/');
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (segments[i] == "match" || segments[i] == "reconnect")
                    return segments[i + 1];
            }
            throw new InvalidOperationException($"Could not find match_id in URL path: {url.AbsolutePath}");
        }

        /// <summary>Connects, sends `authenticate`, and completes the `hello`/`hello` handshake.</summary>
        public async Task ConnectAsync(CancellationToken ct)
        {
            await _ws.ConnectAsync(_url, ct);

            var authenticate = new JsonObject
            {
                ["type"] = "authenticate",
                ["match_id"] = MatchId,
            };
            if (!string.IsNullOrEmpty(_token))
                authenticate["token"] = _token;
            else if (!string.IsNullOrEmpty(_ticket))
                authenticate["ticket"] = _ticket;
            else
                throw new InvalidOperationException("Either a token or a ticket is required to authenticate.");

            await SendAsync(authenticate, ct);

            var hello = await ReceiveJsonAsync(ct)
                ?? throw new InvalidOperationException("Connection closed before server hello.");
            if ((string?)hello["type"] != "hello")
                throw new InvalidOperationException($"Expected server 'hello', got '{(string?)hello["type"]}'.");

            var helloReply = new JsonObject
            {
                ["type"] = "hello",
                ["match_id"] = MatchId,
                ["supported_versions"] = new JsonArray("1.0"),
                ["client_name"] = _clientName,
                ["client_version"] = _clientVersion,
            };
            await SendAsync(helloReply, ct);
        }

        /// <summary>
        /// Runs the Layer 1 receive loop until `match_end` or the socket closes, dispatching
        /// Layer 2 messages to <paramref name="game"/>. Messages are processed strictly one at a
        /// time (no concurrent dispatch), matching the "decide is never re-entrant" contract.
        /// </summary>
        public async Task RunAsync(IChipzenGame game, CancellationToken ct)
        {
            while (true)
            {
                var msg = await ReceiveJsonAsync(ct);
                if (msg is null)
                    return; // socket closed

                var type = (string?)msg["type"];
                switch (type)
                {
                    case "ping":
                        await SendAsync(new JsonObject { ["type"] = "pong", ["match_id"] = MatchId }, ct);
                        break;

                    case "match_start":
                        game.OnMatchStart(msg);
                        break;

                    case "round_start":
                        game.OnRoundStart(msg);
                        break;

                    case "turn_request":
                    {
                        var requestId = (string?)msg["request_id"]
                            ?? throw new InvalidOperationException("turn_request missing request_id");
                        var action = game.OnTurnRequest(msg);
                        await SendTurnActionAsync(requestId, action, ct);
                        break;
                    }

                    case "turn_result":
                        game.OnTurnResult(msg);
                        break;

                    case "phase_change":
                        game.OnPhaseChange(msg);
                        break;

                    case "round_result":
                        game.OnRoundResult(msg);
                        break;

                    case "action_rejected":
                    {
                        var requestId = (string?)msg["request_id"]
                            ?? throw new InvalidOperationException("action_rejected missing request_id");
                        var retry = game.OnActionRejected(msg);
                        await SendTurnActionAsync(requestId, retry, ct);
                        break;
                    }

                    case "match_end":
                        game.OnMatchEnd(msg);
                        return;

                    // action_timeout, error, session_control, session_token, reconnected: informational
                    // for v1, or (per the forward-compatibility rule) simply unhandled/unknown types.
                    default:
                        break;
                }
            }
        }

        private Task SendTurnActionAsync(string requestId, ChipzenAction action, CancellationToken ct)
        {
            var turnAction = new JsonObject
            {
                ["type"] = "turn_action",
                ["match_id"] = MatchId,
                ["request_id"] = requestId,
                ["action"] = action.Action,
            };
            if (action.Amount is int amount)
                turnAction["params"] = new JsonObject { ["amount"] = amount };

            return SendAsync(turnAction, ct);
        }

        private async Task SendAsync(JsonNode message, CancellationToken ct)
        {
            var bytes = Encoding.UTF8.GetBytes(message.ToJsonString());
            await _ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
        }

        private async Task<JsonObject?> ReceiveJsonAsync(CancellationToken ct)
        {
            var buffer = new byte[ReceiveBufferSize];
            using var stream = new System.IO.MemoryStream();

            while (true)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await _ws.ReceiveAsync(buffer, ct);
                }
                catch (WebSocketException)
                {
                    return null;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                    return null;

                stream.Write(buffer, 0, result.Count);

                if (result.EndOfMessage)
                {
                    var text = Encoding.UTF8.GetString(stream.ToArray());
                    return JsonNode.Parse(text) as JsonObject;
                }
            }
        }

        public void Dispose()
        {
            _ws.Dispose();
        }
    }
}
