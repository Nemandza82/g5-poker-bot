using System;

namespace G5.Logic
{
    public class HeroAction
    {
        public ActionType Type;
        public RaiseAmount Amount;
        public int ReactionTime;
        public DateTime IssueTime;

        public HeroAction()
        {
            IssueTime = DateTime.Now;
        }

        public override string ToString()
        {
            var str = Type.ToString();
            return (Type == ActionType.Bet || Type == ActionType.Raise || Type == ActionType.AllIn) ? (str + " " + Amount) : str;
        }
    };
}
