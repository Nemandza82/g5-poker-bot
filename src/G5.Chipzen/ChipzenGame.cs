using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using G5.Logic;
using G5.Logic.Estimators;

namespace G5.Chipzen
{
    /// <summary>
    /// Layer 2 (poker) adapter: the Chipzen analogue of G5.Acpc's AcpcGame. Owns one BotGameState
    /// and one OpponentModeling instance for the lifetime of a single match (this process's
    /// container is expected to play exactly one match, per Chipzen's per-match container model),
    /// translating Chipzen's wire messages to/from G5.Logic calls.
    /// </summary>
    public sealed class ChipzenGame : IChipzenGame, IDisposable
    {
        private readonly Task<OpponentModeling> _opponentModelingTask;

        private BotGameState? _botGameState;
        private int _heroInd = -1;
        private int _bigBlindSize;

        // Chipzen reports raise/all_in `amount` as a per-street total (resets to 0 each new
        // street) rather than a hand-cumulative total (confirmed against the worked example in
        // POKER-GAME-STATE-PROTOCOL.md section 4: a flop raise to "amount: 40" only costs the
        // actor 40 chips, not 40 minus their preflop contribution). G5.Logic's BotGameState, by
        // contrast, tracks Player.MoneyInPot as hand-cumulative (never reset except at hand
        // start). This array is our own per-street running total, indexed by seat, used to
        // convert between the two conventions in both directions.
        private readonly int[] _streetContribution = new int[2];

        public ChipzenGame(Task<OpponentModeling> opponentModelingTask)
        {
            _opponentModelingTask = opponentModelingTask;
        }

        private BotGameState Bgs => _botGameState ?? throw new InvalidOperationException("Match has not started yet.");

        public void OnMatchStart(JsonObject message)
        {
            foreach (var seatObj in message["seats"]!.AsArray())
            {
                if ((bool)seatObj!["is_self"]!)
                    _heroInd = (int)seatObj["seat"]!;
            }
            if (_heroInd < 0)
                throw new InvalidOperationException("match_start did not mark any seat as is_self.");

            _bigBlindSize = (int)message["game_config"]!["big_blind"]!;

            Console.WriteLine($"Match started. Hero seat: {_heroInd}, big blind: {_bigBlindSize}");
        }

        public void OnRoundStart(JsonObject message)
        {
            var state = message["state"]!;
            var stacks = state["stacks"]!.AsArray().Select(x => (int)x!).ToArray();
            int dealerSeat = (int)state["dealer_seat"]!;

            if (_botGameState is null)
            {
                // Blocks only if the background stats-file load (started at process startup, in
                // parallel with the WebSocket connect/handshake) hasn't finished yet.
                var opponentModeling = _opponentModelingTask.GetAwaiter().GetResult();

                var playerNames = new string[stacks.Length];
                playerNames[_heroInd] = "Hero";
                playerNames[1 - _heroInd] = "Villain";

                _botGameState = new BotGameState(playerNames, stacks, _heroInd, dealerSeat, _bigBlindSize,
                    PokerClient.Chipzen, TableType.HeadsUp,
                    new ModelingEstimator(opponentModeling, PokerClient.Chipzen));
            }
            else
            {
                var players = _botGameState.getPlayers();
                for (int i = 0; i < stacks.Length; i++)
                    players[i].SetStackSize(stacks[i]);

                _botGameState.setButtonInd(dealerSeat);
            }

            _botGameState.startNewHand();

            var holeCardStrs = state["your_hole_cards"]!.AsArray().Select(x => (string)x!).ToArray();
            _botGameState.dealHoleCards(new HoleCards(holeCardStrs[0] + holeCardStrs[1]));

            // startNewHand() already posted blinds internally (dealer/button posts the small
            // blind and acts first preflop in heads-up, per both G5's own convention and
            // Chipzen's Section 5.3 rule), so preflop's street-contribution baseline starts
            // seeded with the blinds rather than zero.
            _streetContribution[dealerSeat] = _bigBlindSize / 2;
            _streetContribution[1 - dealerSeat] = _bigBlindSize;

            Console.WriteLine($"Hand {(int)state["hand_number"]!} started. Dealer seat: {dealerSeat}, stacks: [{string.Join(", ", stacks)}]");
        }

        public ChipzenAction OnTurnRequest(JsonObject message)
        {
            var state = message["state"]!;
            var validActions = message["valid_actions"]!.AsArray().Select(x => (string)x!).ToHashSet();

            var bd = Bgs.calculateHeroAction();
            Console.WriteLine($"Bot decision: {bd.actionType} byAmount={bd.byAmount} ({bd.timeSpentSeconds:f2}s)");

            if (bd.actionType == ActionType.NoAction)
            {
                Console.WriteLine("WARNING: server asked us to act but internal state disagrees: " + bd.message);
                return SafeFallback(validActions);
            }

            switch (bd.actionType)
            {
                case ActionType.Fold:
                    return ChipzenAction.Fold();

                case ActionType.Check:
                case ActionType.Call:
                    return validActions.Contains("check") ? ChipzenAction.Check() : ChipzenAction.Call();

                case ActionType.AllIn:
                    return ChipzenAction.AllIn();

                case ActionType.Raise:
                {
                    if (!validActions.Contains("raise"))
                        return validActions.Contains("call") ? ChipzenAction.Call() : SafeFallback(validActions);

                    int minRaise = (int)state["min_raise"]!;
                    int maxRaise = (int)state["max_raise"]!;
                    // min_raise/max_raise are street-relative totals (see the _streetContribution
                    // comment above), so bd.byAmount (a hand-cumulative-relative increment) must
                    // be added onto our own street-so-far total, not onto MoneyInPot.
                    int total = _streetContribution[_heroInd] + bd.byAmount;
                    total = Math.Clamp(total, minRaise, maxRaise);

                    return total >= maxRaise ? ChipzenAction.AllIn() : ChipzenAction.Raise(total);
                }

                default:
                    return SafeFallback(validActions);
            }
        }

        private static ChipzenAction SafeFallback(HashSet<string> validActions) =>
            validActions.Contains("check") ? ChipzenAction.Check() : ChipzenAction.Fold();

        public void OnTurnResult(JsonObject message)
        {
            int seat = (int)message["seat"]!;
            if (Bgs.getPlayerToActInd() != seat)
                throw new InvalidOperationException(
                    $"turn_result for seat {seat} but internal state expects seat {Bgs.getPlayerToActInd()}.");

            var details = message["details"]!;
            string action = (string)details["action"]!;
            int amount = (int)details["amount"]!;

            var actionType = action switch
            {
                "fold" => ActionType.Fold,
                "check" => ActionType.Check,
                "call" => ActionType.Call,
                "raise" => ActionType.Raise,
                "all_in" => ActionType.AllIn,
                _ => throw new InvalidOperationException($"Unknown action in turn_result: {action}"),
            };

            // raise/all_in `amount` is this player's new per-street total (see the
            // _streetContribution field comment); call's `amount` is already the increment paid
            // (confirmed against the protocol's worked example, where a call's reported amount
            // matched the call cost, not the resulting street total). playerActs always wants
            // an increment.
            int byAmount = actionType switch
            {
                ActionType.Raise or ActionType.AllIn => amount - _streetContribution[seat],
                ActionType.Call => amount,
                _ => 0, // fold, check
            };

            Bgs.playerActs(actionType, byAmount);
            _streetContribution[seat] += byAmount;
        }

        public void OnPhaseChange(JsonObject message)
        {
            var board = message["state"]!["board"]!.AsArray().Select(x => (string)x!).ToArray();

            int alreadyDealt = Bgs.getBoard().Count;
            var newCards = board.Skip(alreadyDealt).Select(s => new Card(s)).ToList();
            if (newCards.Count == 0)
                return;

            Bgs.goToNextStreet(newCards);
            Array.Clear(_streetContribution);
        }

        public void OnRoundResult(JsonObject message)
        {
            var result = message["result"]!;
            var players = Bgs.getPlayers();
            var winnings = new List<int>(new int[players.Count]);

            foreach (var payout in result["payouts"]!.AsArray())
            {
                int seat = (int)payout!["seat"]!;
                winnings[seat] = (int)payout["amount"]!;
            }

            Bgs.finishHand(winnings);
            _opponentModelingTask.GetAwaiter().GetResult().addHand(Bgs.getCurrentHand());
        }

        public ChipzenAction OnActionRejected(JsonObject message)
        {
            var validActions = (message["valid_actions"] as JsonArray)?.Select(x => (string)x!).ToHashSet()
                ?? new HashSet<string> { "check", "fold" };

            Console.WriteLine("WARNING: action_rejected: " + (string?)message["message"]);
            return SafeFallback(validActions);
        }

        public void OnMatchEnd(JsonObject message)
        {
            Console.WriteLine("Match ended: " + (string?)message["reason"]);
        }

        public void Dispose()
        {
            _botGameState?.Dispose();
            _botGameState = null;
        }
    }
}
