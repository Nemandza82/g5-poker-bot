using System.IO;


namespace G5.Logic
{
    public class ActionStats
    {
        public int BetRaiseSamples { get; set; }
        public int CheckCallSamples { get; set; }
        public int FoldSamples { get; set; }

        public ActionStats()
        {
            BetRaiseSamples = 0;
            CheckCallSamples = 0;
            FoldSamples = 0;
        }

        public void serialize(BinaryWriter writer)
        {
            writer.Write(BetRaiseSamples);
            writer.Write(CheckCallSamples);
            writer.Write(FoldSamples);
        }

        public static ActionStats deserialize(BinaryReader reader)
        {
            ActionStats ad = new ActionStats();

            ad.BetRaiseSamples = reader.ReadInt32();
            ad.CheckCallSamples = reader.ReadInt32();
            ad.FoldSamples = reader.ReadInt32();

            return ad;
        }

        public int totalSamples()
        {
            return BetRaiseSamples + CheckCallSamples + FoldSamples;
        }

        public void addSample(ActionType actionType)
        {
            if (actionType == ActionType.Fold)
            {
                FoldSamples++;
            }
            else if (actionType == ActionType.Call || actionType == ActionType.Check)
            {
                CheckCallSamples++;
            }
            else if (actionType == ActionType.AllIn ||
                     actionType == ActionType.Bet ||
                     actionType == ActionType.Raise)
            {
                BetRaiseSamples++;
            }
        }

        public void append(ActionStats asv)
        {
            BetRaiseSamples += asv.BetRaiseSamples;
            CheckCallSamples += asv.CheckCallSamples;
            FoldSamples += asv.FoldSamples;
        }

        public void clear()
        {
            BetRaiseSamples = 0;
            CheckCallSamples = 0;
            FoldSamples = 0;
        }

        public float betRaiseProbability()
        {
            return (totalSamples() > 0) ? (BetRaiseSamples / (float)totalSamples()) : 0.33f;
        }

        public float checkCallProbability()
        {
            return (totalSamples() > 0) ? (CheckCallSamples / (float)totalSamples()) : 0.33f;
        }

        public float foldProbability()
        {
            return (totalSamples() > 0) ? (FoldSamples / (float)totalSamples()) : 0.33f;
        }

        public override string ToString()
        {
            return "BR: " + betRaiseProbability().ToString("f2") +
                    ", CC: " + checkCallProbability().ToString("f2") +
                    ", FO: " + foldProbability().ToString("f2") + " [" + totalSamples() + "]";
        }
    }
}
