namespace G5.Logic
{
    public struct Action
    {
        public Street Street;
        public string PlayerName;
        public ActionType Type;

        /// <summary>
        /// Raise by ammount.
        /// </summary>
        public int Amount;

        public Action(Street aStreet, string aPlayerName, ActionType aType, int aAmmount)
        {
            Street = aStreet;
            PlayerName = aPlayerName;
            Type = aType;
            Amount = aAmmount;
        }

        public dynamic toDynamic()
        {
            string typeToRet = "";

            if (Type == ActionType.Fold)
                typeToRet = "f";
            else if (IsRaiseAction)
                typeToRet = "br";
            else
                typeToRet = "cc";

            return new { type = typeToRet, ammount = Amount };
        }

        public bool IsRaiseAction
        {
            get
            {
                return Type == ActionType.Raise ||
                       Type == ActionType.Bet ||
                       Type == ActionType.AllIn;
            }
        }

        public bool IsValidAction
        {
            get
            {
                bool isValid = IsRaiseAction ||
                     Type == ActionType.Call ||
                     Type == ActionType.Check ||
                     Type == ActionType.Fold;

                return isValid;
            }
        }

        override public string ToString()
        {
            string str = Street + ": ";

            if (Type == ActionType.AllIn)
                str += PlayerName + " " + "went all in " + Amount;

            if (Type == ActionType.Bet)
                str += PlayerName + " " + "bet " + Amount;

            if (Type == ActionType.Call)
                str += PlayerName + " " + "called " + Amount;

            if (Type == ActionType.Raise)
                str += PlayerName + " " + "raised " + Amount;

            if (Type == ActionType.Fold)
                str += PlayerName + " " + "folded";

            if (Type == ActionType.Check)
                str += PlayerName + " " + "checked";

            if (Type == ActionType.Wins)
                str += PlayerName + " " + "wins " + Amount;

            if (Type == ActionType.MoneyReturned)
                str += "money returned to " + PlayerName + " " + Amount;

            return str + "; ";
        }

        private string AmountToString()
        {
            var dec = Amount / 100.0m;
            return "€" + dec;
        }
    };
}
