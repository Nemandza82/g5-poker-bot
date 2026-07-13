using System.Text.Json.Nodes;

namespace G5.Chipzen
{
    /// <summary>
    /// Layer 2 (poker) callbacks dispatched by <see cref="ChipzenClient"/>'s Layer 1 message loop.
    /// One JSON object per method, matching the Chipzen wire message of the same name.
    /// </summary>
    public interface IChipzenGame
    {
        void OnMatchStart(JsonObject message);
        void OnRoundStart(JsonObject message);

        /// <summary>Called only when this seat is being asked to act. Returns the action to send.</summary>
        ChipzenAction OnTurnRequest(JsonObject message);

        void OnTurnResult(JsonObject message);
        void OnPhaseChange(JsonObject message);
        void OnRoundResult(JsonObject message);
        void OnMatchEnd(JsonObject message);

        /// <summary>The previous action was rejected; produce a corrected retry for the same request_id.</summary>
        ChipzenAction OnActionRejected(JsonObject message);
    }

    /// <summary>A `turn_action` payload: one of fold/check/call/raise/all_in, with an optional total-bet amount.</summary>
    public readonly struct ChipzenAction
    {
        public string Action { get; }
        public int? Amount { get; }

        public ChipzenAction(string action, int? amount = null)
        {
            Action = action;
            Amount = amount;
        }

        public static ChipzenAction Fold() => new ChipzenAction("fold");
        public static ChipzenAction Check() => new ChipzenAction("check");
        public static ChipzenAction Call() => new ChipzenAction("call");
        public static ChipzenAction Raise(int totalAmount) => new ChipzenAction("raise", totalAmount);
        public static ChipzenAction AllIn() => new ChipzenAction("all_in");
    }
}
