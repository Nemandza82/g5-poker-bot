using System.Diagnostics;


namespace G5.Logic
{
    public class Player
    {
        public string Name { get; private set; }
        public Status StatusInHand { get; set; }
        public ActionType LastAction { get; private set; }
        public ActionType PrevStreetAction { get; private set; }
        public Position PreFlopPosition { get; set; }

        public Range Range { get; private set; }
        public PlayerModel Model { get; set; }

        public int Stack { get; private set; }
        public int MoneyInPot { get; private set; }
        public int MoneyWon { get; private set; }

        public Player(string aName, int stack, PlayerModel playerModel)
        {
            Name = aName;
            Stack = stack;
            MoneyInPot = 0;
            MoneyWon = 0;

            StatusInHand = Status.ToAct;
            LastAction = ActionType.Fold;
            PrevStreetAction = ActionType.Fold;
            PreFlopPosition = Position.UTG;

            Range = new Range();
            Model = playerModel;
        }

        public Player(Player oldPlayer)
        {
            Name = oldPlayer.Name;
            Stack = oldPlayer.Stack;
            MoneyInPot = oldPlayer.MoneyInPot;
            MoneyWon = oldPlayer.MoneyWon;

            StatusInHand = oldPlayer.StatusInHand;
            LastAction = oldPlayer.LastAction;
            PrevStreetAction = oldPlayer.PrevStreetAction;
            PreFlopPosition = oldPlayer.PreFlopPosition;

            Range = new Range(oldPlayer.Range);
            Model = new PlayerModel(oldPlayer.Model);
        }

        public void SetStackSize(int size)
        {
            Stack = size;
        }

        public void SetPlayerName(string name)
        {
            Name = name;
        }

        public void BringsIn(int amount)
        {
            Stack += amount;
        }

        public void Posts(int amount)
        {
            Debug.Assert(StatusInHand == Status.ToAct);
            StatusInHand = Status.ToAct;
            MoneyInPot += amount;
            Stack -= amount;
        }

        public void Folds()
        {
            Debug.Assert(StatusInHand == Status.ToAct);
            StatusInHand = Status.Folded;
            LastAction = ActionType.Fold;
        }

        public void Checks()
        {
            Debug.Assert(StatusInHand == Status.ToAct);
            StatusInHand = Status.Acted;
            LastAction = ActionType.Check;
        }

        public void Calls(int amount)
        {
            Debug.Assert(StatusInHand == Status.ToAct);
            StatusInHand = Status.Acted;
            LastAction = ActionType.Call;

            MoneyInPot += amount;
            Stack -= amount;
        }

        public void BetsOrRaisesTo(int toAmount)
        {
            Debug.Assert(StatusInHand == Status.ToAct);
            StatusInHand = Status.Acted;
            LastAction = ActionType.Raise;

            int amount = toAmount - MoneyInPot;
            MoneyInPot = toAmount;
            Stack -= amount;

            Debug.Assert(amount > 0);
        }

        public void GoesAllIn()
        {
            GoesAllIn(Stack);
        }

        public void GoesAllIn(int amount)
        {
            Debug.Assert(StatusInHand == Status.ToAct);
            StatusInHand = Status.AllIn;
            LastAction = ActionType.AllIn;

            MoneyInPot += amount;
            Stack = 0;

            Debug.Assert(amount > 0);
        }

        public void MoneyReturned(int amount)
        {
            //Debug.Assert(statusInHand == Status.Acted || statusInHand == Status.WentAllIn);
            MoneyInPot -= amount;
            Stack += amount;
        }

        public void WinsHand(int amount)
        {
            //Debug.Assert(statusInHand == Status.Acted || statusInHand == Status.WentAllIn);
            MoneyWon = amount;
            Stack += amount;
        }

        public void NextStreet()
        {
            PrevStreetAction = LastAction;
            LastAction = ActionType.Fold;
        }

        public void ResetHand()
        {
            StatusInHand = Status.ToAct;
            LastAction = ActionType.Fold;
            PrevStreetAction = ActionType.Fold;
            Range.Reset();

            MoneyInPot = 0;
            MoneyWon = 0;
        }

        public int Round()
        {
            return (LastAction == ActionType.Fold) ? 0 : 1;
        }

        public EstimatedAD GetAD(PreFlopParams prms)
        {
            return Model.GetPreFlopAD(prms.ToIndex());
        }

        public EstimatedAD GetAD(PostFlopParams prms)
        {
            return Model.GetPostFlopAD(prms.ToIndex());
        }

        public void CutRange(ActionType actionType, Street currentStreet, Board board, float betRaiseChance, float checkCallChance, DecisionMakingContext dmContext)
        {
            if (actionType == ActionType.Check || actionType == ActionType.Bet)
            {
                Range.CutCheckBet(actionType, currentStreet, board, betRaiseChance, dmContext);
            }
            else if (actionType == ActionType.Call || actionType == ActionType.Raise || actionType == ActionType.AllIn)
            {
                Range.CutFoldCallRaise(actionType, currentStreet, board, betRaiseChance, checkCallChance, dmContext);
            }
        }

        public void BanCardInRange(Card card, bool isBoard)
        {
            Range.BanCard(card, isBoard);
        }

        override public string ToString()
        {
            string tabs0 = (Name.Length > 7) ? "\t" : "\t\t";
            return Name + tabs0 + "[" + StatusInHand.ToString() + "] [" + Stack + "] "
                                           + "[" + MoneyInPot + "]";
        }
    }
}
