using System;
using System.Collections.Generic;
using System.Diagnostics;



namespace G5.Logic
{
    public struct PreFlopParams
    {
        public TableType TableType { get; private set; }
        public Position Position { get; private set; }
        public int NumCallers { get; private set; }
        public int NumRaises { get; private set; }
        public int NumActivePlayers { get; private set;  }
        public ActionType PreviousAction { get; private set; }
        public bool InPositionOnFlop { get; private set; }

        public PreFlopParams(TableType tableType, Position position, int numCallers, int numRaises, int numActivePlayers,
            ActionType previousAction, bool inPositionOnFlop) : this()
        {
            TableType = tableType;
            Position = position;
            NumCallers = numCallers;
            NumRaises = numRaises;
            NumActivePlayers = numActivePlayers;
            PreviousAction = previousAction;
            InPositionOnFlop = inPositionOnFlop;
        }

        public bool ForcedAction()
        {
            bool forced = true;

            if (Position == Position.BigBlind)
            {
                forced = NumRaises > 0;
            }

            return forced;
        }

        public static List<PreFlopParams> getAllParams(TableType tableType)
        {
            var allParams = new List<PreFlopParams>();

            if (tableType == TableType.HeadsUp)
            {
                for (int numRaises = 0; numRaises <= 4; numRaises++)
                    allParams.Add(new PreFlopParams(tableType, Position.SmallBlind, 0, numRaises, 2, ActionType.Fold, true));

                for (int numRaises = 0; numRaises <= 4; numRaises++)
                    allParams.Add(new PreFlopParams(tableType, Position.BigBlind, 0, numRaises, 2, ActionType.Fold, false));
            }
            else
            {
                foreach (Position pos in Enum.GetValues(typeof(Position)))
                {
                    allParams.Add(new PreFlopParams(tableType, pos, 0, 0, (int)tableType, ActionType.Fold, false));
                    allParams.Add(new PreFlopParams(tableType, pos, 1, 0, (int)tableType, ActionType.Fold, false));
                    allParams.Add(new PreFlopParams(tableType, pos, 0, 1, (int)tableType, ActionType.Fold, false));
                    allParams.Add(new PreFlopParams(tableType, pos, 1, 1, (int)tableType, ActionType.Fold, false));
                    allParams.Add(new PreFlopParams(tableType, pos, 0, 2, (int)tableType, ActionType.Fold, false));
                }

                ActionType[] prevActions = { ActionType.Check, ActionType.Raise };

                foreach (var previousAction in prevActions)
                {
                    for (int inPos = 0; inPos <= 1; inPos++)
                    {
                        for (int numPl = 2; numPl <= 3; numPl++)
                        {
                            allParams.Add(new PreFlopParams(tableType, Position.BigBlind, 0, 1, numPl, previousAction, inPos != 0));
                            allParams.Add(new PreFlopParams(tableType, Position.BigBlind, 1, 1, numPl, previousAction, inPos != 0));
                            allParams.Add(new PreFlopParams(tableType, Position.BigBlind, 0, 2, numPl, previousAction, inPos != 0));
                        }
                    }
                }
            }

            return allParams;
        }

        public int ToIndex()
        {
            int index;

            if (TableType == TableType.HeadsUp)
            {
                if (Position == Position.SmallBlind)
                {
                    index = Math.Min(NumRaises, 4);
                }
                else if (Position == Position.BigBlind)
                {
                    index = 5 + Math.Min(NumRaises, 4);
                }
                else
                {
                    throw new Exception("Wrong position for HU table: " + Position);
                }
            }
            else if (PreviousAction == ActionType.Fold) // Prvi krug, prvih 30 pozicija...
            {
                int a0 = (int)Position;
                int a1 = -1;

                if (NumRaises == 0)
                {
                    a1 = (NumCallers == 0) ? 0 : a1;
                    a1 = (NumCallers > 0) ? 1 : a1;
                }
                else if (NumRaises == 1)
                {
                    a1 = (NumCallers == 0) ? 2 : a1;
                    a1 = (NumCallers > 0) ? 3 : a1;
                }
                else if (NumRaises >= 2)
                {
                    a1 = 4;
                }
 
                Debug.Assert((a0 != -1) && (a1 != -1));
                index = (5 * a0) + a1; 
            }
            else // Drugi krug. PreviousAction: Check/Call, Raise/Bet
            {
                int a0 = -1;
                int a1 = -1;
                int a2 = -1;
                int a3 = -1;

                a0 = (PreviousAction == ActionType.Check || PreviousAction == ActionType.Call) ? 0 : a0;
                a0 = (PreviousAction == ActionType.Bet || PreviousAction == ActionType.Raise) ? 1 : a0;

                a1 = InPositionOnFlop ? 0 : 1;

                a2 = (NumActivePlayers == 2) ? 0 : a2;
                a2 = (NumActivePlayers > 2) ? 1 : a2;

                if (NumRaises == 1)
                {
                    a3 = (NumCallers == 0) ? 0 : a3;
                    a3 = (NumCallers > 0) ? 1 : a3;
                }
                else if (NumRaises >= 2)
                {
                    a3 = 2;
                }

                Debug.Assert((a0 != -1) && (a1 != -1) && (a2 != -1) && (a3 != -1));
                index = 30 + (12 * a0) + (6 * a1) + (3 * a2) + a3;
            }

            return index;
        }

        public override string ToString()
        {
            if (PreviousAction == ActionType.Fold)
            {
                return PreviousAction.ToString() + ", " + Position + ", bets: " + NumRaises + ", limpers: " + NumCallers;
            }
            else
            {
                string posTrs = InPositionOnFlop ? "inp" : "oop";
                return PreviousAction.ToString() + ", " + posTrs + ", players: " + NumActivePlayers + ", raises: " + NumRaises + ", callers: " + NumCallers;
            }
        }
    };
}
