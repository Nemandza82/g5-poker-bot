namespace G5.Logic
{
    public struct EstimatedAD
    {
        public GaussianDistribution BetRaise { get; private set; }
        public GaussianDistribution CheckCall { get; private set; }
        public GaussianDistribution Fold { get; private set; }
        public int PriorSamples { get; private set; }
        public int UpdateSamples { get; private set; }

        public EstimatedAD(GaussianDistribution estBetRaise, GaussianDistribution estCheckCall, GaussianDistribution estFold, 
            int priorSamples, int updateSamples) : this()
        {
            BetRaise = estBetRaise;
            CheckCall = estCheckCall;
            Fold = estFold;
            PriorSamples = priorSamples;
            UpdateSamples = updateSamples;
        }

        public override string ToString()
        {
            return "BR: " + BetRaise.Mean.ToString("f2") +
                   ", CC: " + CheckCall.Mean.ToString("f2") +
                   ", FO: " + Fold.Mean.ToString("f2") + 
                   " [" + PriorSamples.ToString() + ", " + UpdateSamples.ToString() + "]";
        }
    }
}
