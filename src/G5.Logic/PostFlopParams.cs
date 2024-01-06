using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace G5.Logic
{
    public struct PostFlopParams
    {
        public TableType TableType { get; private set; }
        public Street Street { get; private set; }
        public int Round { get; private set; }
        public ActionType PrevAction { get; private set; }
        public int NumBets { get; private set; }
        public bool InPosition { get; private set; }
        public int NumPlayers { get; private set; }

        public PostFlopParams(TableType tableType, Street street, int round, ActionType prevAction, int numBets, bool inPosition, int numPlayers) : this()
        {
            TableType = tableType;
            Street = street;
            Round = round;
            PrevAction = prevAction;
            NumBets = numBets;
            InPosition = inPosition;
            NumPlayers = numPlayers;
        }

        public bool ForcedAction()
        {
            return NumBets > 0;
        }

        public static List<PostFlopParams> getAllParams(TableType tableType)
        {
            var allParams = new List<PostFlopParams>();
            Street[] streets = { Street.Flop, Street.Turn, Street.River };

            if (tableType == TableType.HeadsUp)
            {
                foreach (var street in streets) // 15
                {
                    // SB => inPos == true (8)
                    // round == 0, BB checked => numBets == 0
                    allParams.Add(new PostFlopParams(tableType, street, 0, ActionType.Raise, 0, true, 2)); // C 0
                    allParams.Add(new PostFlopParams(tableType, street, 0, ActionType.Call, 0, true, 2)); // C 1
                    allParams.Add(new PostFlopParams(tableType, street, 0, ActionType.Check, 0, true, 2)); // C 2

                    // round == 0, BB raised => numBets == 1
                    allParams.Add(new PostFlopParams(tableType, street, 0, ActionType.Raise, 1, true, 2)); // R 3
                    allParams.Add(new PostFlopParams(tableType, street, 0, ActionType.Call, 1, true, 2)); // R 4
                    allParams.Add(new PostFlopParams(tableType, street, 0, ActionType.Check, 1, true, 2)); // R 5

                    allParams.Add(new PostFlopParams(tableType, street, 1, ActionType.Raise, 2, true, 2)); // CRR 6
                    allParams.Add(new PostFlopParams(tableType, street, 1, ActionType.Raise, 3, true, 2)); // RRR 7

                    // BB => inPos == false (7)
                    // round == 0. First to act
                    allParams.Add(new PostFlopParams(tableType, street, 0, ActionType.Raise, 0, false, 2)); // 0
                    allParams.Add(new PostFlopParams(tableType, street, 0, ActionType.Call, 0, false, 2)); // 1
                    allParams.Add(new PostFlopParams(tableType, street, 0, ActionType.Check, 0, false, 2)); // 2

                    allParams.Add(new PostFlopParams(tableType, street, 1, ActionType.Check, 1, false, 2)); // CR 3
                    allParams.Add(new PostFlopParams(tableType, street, 1, ActionType.Raise, 2, false, 2)); // RR 4
                    allParams.Add(new PostFlopParams(tableType, street, 2, ActionType.Raise, 3, false, 2)); // CRRR 5
                    allParams.Add(new PostFlopParams(tableType, street, 2, ActionType.Raise, 4, false, 2)); // RRRR 6
                }
            }
            else
            {
                ActionType[] prevActions = { ActionType.Raise, ActionType.Call, ActionType.Check };

                foreach (var street in streets)
                {
                    for (int round = 0; round <= 1; round++)
                    {
                        foreach (var prevAction in prevActions)
                        {
                            for (int numBets = 0; numBets <= 2; numBets++)
                            {
                                for (int inPos = 0; inPos <= 1; inPos++)
                                {
                                    for (int numPl = 2; numPl <= 3; numPl++)
                                        allParams.Add(new PostFlopParams(tableType, street, round, prevAction, numBets, inPos != 0, numPl));
                                }
                            }
                        }
                    }
                }
            }

            return allParams;
        }

        public int ToIndex()
        {
            int index = 0;

            int prevActionMod = -1;
            prevActionMod = (PrevAction == ActionType.Bet || PrevAction == ActionType.Raise || PrevAction == ActionType.AllIn) ? 0 : prevActionMod;
            prevActionMod = (PrevAction == ActionType.Call) ? 1 : prevActionMod;
            prevActionMod = (PrevAction == ActionType.Check) ? 2 : prevActionMod;

            if (prevActionMod == -1)
            {
                Console.WriteLine($"Warninig!: prevActionMod is -1, prevAction: {prevActionMod}");
                prevActionMod = 0;
            }

            if (TableType == TableType.HeadsUp)
            {
                Debug.Assert(Street == Street.Flop || Street == Street.Turn || Street == Street.River);

                if (Street == Street.Turn)
                    index = 15;

                if (Street == Street.River)
                    index = 30;

                if (InPosition) // SB
                {
                    if (NumBets < 2)
                    {
                        index += (NumBets == 0) ? prevActionMod : (3 + prevActionMod);
                    }
                    else
                    {
                        index += (NumBets == 2) ? 6 : 7;
                    }
                }
                else // BB
                {
                    index += 8;
                    index += (NumBets == 0) ? prevActionMod : (Math.Min(NumBets, 4) + 2);
                }
            }
            else
            {
                int a0 = -1;

                a0 = (Street == Street.Flop) ? 0 : a0;
                a0 = (Street == Street.Turn) ? 1 : a0;
                a0 = (Street == Street.River) ? 2 : a0;

                if (a0 == -1)
                {
                    Console.WriteLine($"Warninig!: a0 is -1, Street: {Street}");
                    a0 = 0;
                }

                int a1 = (Round == 0) ? 0 : 1;

                var a3 = 0;
                a3 = (NumBets <= 0) ? 0 : a3;
                a3 = (NumBets == 1) ? 1 : a3;
                a3 = (NumBets >= 2) ? 2 : a3;

                int a4 = InPosition ? 1 : 0;

                var a5 = 0;
                a5 = (NumPlayers <= 2) ? 0 : a5;
                a5 = (NumPlayers >= 3) ? 1 : a5;

                index = a5 + (2 * a4) + (4 * a3) + (12 * prevActionMod) + (36 * a1) + (72 * a0);
            }

            return index;
        }

        public override string ToString()
        {
            string posStr = InPosition ? "inp" : "oop";
            return Street.ToString() + ", round: " + Round + " " + PrevAction + ", bets: " + NumBets + ", " + posStr + ", pl: " + NumPlayers;
        }
    };
}
