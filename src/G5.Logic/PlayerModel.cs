namespace G5.Logic
{
    public class PlayerModel
    {
        public TableType TableType { get; private set; }

        public GaussianDistribution VPIP { get; private set; }
        public GaussianDistribution PFR { get; private set; }
        public GaussianDistribution WTP { get; private set; }
        public GaussianDistribution AGG { get; private set; }

        public EstimatedAD[] PreFlopAD { get; private set; }
        public EstimatedAD[] PostFlopAD { get; private set; }

        public PlayerModel(TableType tableType, GaussianDistribution vpip, GaussianDistribution pfr, GaussianDistribution wtp, GaussianDistribution agg,
            EstimatedAD[] preFlopAD, EstimatedAD[] postFlopAD)
        {
            TableType = tableType;

            VPIP = vpip;
            PFR = pfr;
            WTP = wtp;
            AGG = agg;

            PreFlopAD = preFlopAD;
            PostFlopAD = postFlopAD;
        }

        public PlayerModel(PlayerModel oldModel)
        {
            TableType = oldModel.TableType;

            VPIP = oldModel.VPIP;
            PFR = oldModel.PFR;
            WTP = oldModel.WTP;
            AGG = oldModel.AGG;

            PreFlopAD = new EstimatedAD[oldModel.PreFlopAD.Length];
            PostFlopAD = new EstimatedAD[oldModel.PostFlopAD.Length];

            for (var i = 0; i < PreFlopAD.Length; i++)
                PreFlopAD[i] = oldModel.PreFlopAD[i];

            for (var i = 0; i < PostFlopAD.Length; i++)
                PostFlopAD[i] = oldModel.PostFlopAD[i];
        }

        public EstimatedAD GetPreFlopAD(int index)
        {
            return PreFlopAD[index];
        }

        public EstimatedAD GetPostFlopAD(int index)
        {
            return PostFlopAD[index];
        }

        public override string ToString()
        {
            string str = "";

            str += "VPIP: " + VPIP.ToString() + "\n";
            str += "PFR: " + PFR.ToString() + "\n";
            str += "WTP: " + WTP.ToString() + "\n";
            str += "Aggression: " + AGG.ToString() + "\n\n";
            
            return str + paramsToString();
        }

        public string paramsToString()
        {
            string str = "";

            var allPreFlopParams = PreFlopParams.getAllParams(TableType);

            for (int i = 0; i < allPreFlopParams.Count; i++)
            {
                str += allPreFlopParams[i].ToString() + "; ";
                str += PreFlopAD[i].ToString() + "\n";
            }

            var allPostFlopParams = PostFlopParams.getAllParams(TableType);

            for (int i = 0; i < allPostFlopParams.Count; i++)
            {
                str += allPostFlopParams[i].ToString() + "; ";
                str += PostFlopAD[i].ToString() + "\n";
            }

            return str;
        }
    };
}
