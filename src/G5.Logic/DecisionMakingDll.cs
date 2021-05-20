using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System;


namespace G5.Logic
{
    public class DecisionMakingDll
    {
        private const int N_HOLECARDS = 52 * 51 / 2;

        private struct Unmanaged_Range
        {
            public int length;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = N_HOLECARDS)]
            public int[] ind;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = N_HOLECARDS)]
            public float[] equity;

            public Unmanaged_Range(Range range)
            {
                length = range.Data.Length;
                ind = new int[N_HOLECARDS];
                equity = new float[N_HOLECARDS];

                for (int i = 0; i < length; i++)
                {
                    ind[i] = range.Data[i].Ind;
                    equity[i] = range.Data[i].Equity;
                }
            }

            public void ToManaged(Range range)
            {
                Debug.Assert(range.Data.Length == length);

                for (int i = 0; i < range.Data.Length; i++)
                {
                    range.Data[i].Ind = ind[i];
                    range.Data[i].Equity = equity[i];
                }
            }
        };

        private struct Unmanaged_ActionDistribution
        {
            public float betRaiseProb;
            public float checkCallProb;
            public float foldProb;

            public Unmanaged_ActionDistribution(EstimatedAD ad)
            {
                betRaiseProb = ad.BetRaise.Mean;
                checkCallProb = ad.CheckCall.Mean;
                foldProb = ad.Fold.Mean;
            }
        };

        public const int MAX_MODEL_PARAMS = 256;

        private struct Unmanaged_OpponentModel
        {
            public int totalPlayers;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_MODEL_PARAMS)]
            public Unmanaged_ActionDistribution[] preFlopAD;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_MODEL_PARAMS)]
            public Unmanaged_ActionDistribution[] postFlopAD;

            public Unmanaged_OpponentModel(PlayerModel model)
            {
                totalPlayers = (int)model.TableType;

                preFlopAD = new Unmanaged_ActionDistribution[MAX_MODEL_PARAMS];
                postFlopAD = new Unmanaged_ActionDistribution[MAX_MODEL_PARAMS];

                for (int i = 0; i < model.PreFlopAD.Length; i++)
                {
                    preFlopAD[i] = new Unmanaged_ActionDistribution(model.PreFlopAD[i]);
                }

                for (int i = 0; i < model.PostFlopAD.Length; i++)
                {
                    postFlopAD[i] = new Unmanaged_ActionDistribution(model.PostFlopAD[i]);
                }
            }
        };

        struct Unmanaged_Card
        {
            public int rank;
            public int suite;
            public int value;

            public Unmanaged_Card(Card card)
            {
                rank = (int)card.rank;
                suite = (int)card.suite;
                value = card.ToInt();
            }
        };

        struct Unmanaged_HoleCards
        {
            public Unmanaged_Card Card0;
            public Unmanaged_Card Card1;

            public Unmanaged_HoleCards(HoleCards holeCards)
            {
                Card0 = new Unmanaged_Card(holeCards.Card0);
                Card1 = new Unmanaged_Card(holeCards.Card1);
            }
        };

        struct Unmanaged_Player
        {
            public int id;
            public int statusInHand;
            public int lastAction;
            public int prevStreetAction;
            public int preFlopPosition;

            public int stack;
            public int moneyInPot;

            public Unmanaged_OpponentModel model;
            public Unmanaged_Range range;

            public Unmanaged_Player(Player player, int ind)
            {
                id = ind;
                statusInHand = (int)player.StatusInHand;
                lastAction = (int)player.LastAction;
                prevStreetAction = (int)player.PrevStreetAction;
                preFlopPosition = (int)player.PreFlopPosition;
                stack = player.Stack;
                moneyInPot = player.MoneyInPot;

                model = new Unmanaged_OpponentModel(player.Model);
                range = new Unmanaged_Range(player.Range);
            }
        };

        private static Unmanaged_Card[] BoardToUnmanaged(Board board)
        {
            Unmanaged_Card[] cards = new Unmanaged_Card[5];

            if (board != null)
            {
                for (int i = 0; i < board.Count; i++)
                {
                    cards[i] = new Unmanaged_Card(board.Cards[i]);
                }
            }

            return cards;
        }


        [DllImport("DecisionMaking.dll", EntryPoint = "CreateGameContext", CharSet = CharSet.Ansi)]
        public static extern IntPtr CreateGameContext();


        [DllImport("DecisionMaking.dll", EntryPoint = "ReleaseGameContext", CharSet = CharSet.Ansi)]
        public static extern void ReleaseGameContext(IntPtr gc);


        [DllImport("DecisionMaking.dll", EntryPoint = "GameContext_NewFlop", CharSet = CharSet.Ansi)]
        private static extern void GameContext_NewFlop(IntPtr gc, string strFlop0, string strFlop1, string strFlop2, string strHoleCards);

        public static void GameContext_NewFlop(DecisionMakingContext dmContext, Board board, HoleCards heroHoleCards)
        {
            GameContext_NewFlop(dmContext.GC,
                board.Flop[0].ToString(),
                board.Flop[1].ToString(),
                board.Flop[2].ToString(),
                (heroHoleCards != null) ? heroHoleCards.ToString() : null);
        }


        [DllImport("DecisionMaking.dll", EntryPoint = "Range_GetSortedHoleCards", CharSet = CharSet.Ansi)]
        private static extern void Range_GetSortedHoleCards(ref Unmanaged_Range range, int street,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]Unmanaged_Card[] board, IntPtr gc);

        public static void Range_GetSortedHoleCards(Range range, Street street, Board board, DecisionMakingContext dmContext)
        {
            Unmanaged_Range unmanagedRange = new Unmanaged_Range(range);
            Unmanaged_Card[] cards = BoardToUnmanaged(board);

            Range_GetSortedHoleCards(ref unmanagedRange, (int)street, cards, dmContext.GC);
            unmanagedRange.ToManaged(range);
        }


        [DllImport("DecisionMaking.dll", EntryPoint = "CutRange_CheckBet", CharSet = CharSet.Ansi)]
        private static extern void CutRange_CheckBet(ref Unmanaged_Range range, int actionType, int street,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]Unmanaged_Card[] board, float betChance, IntPtr gc);

        public static void CutRange_CheckBet(Range range, ActionType actionType, Street street, Board board, float betChance, DecisionMakingContext dmContext)
        {
            Unmanaged_Range unmanagedRange = new Unmanaged_Range(range);
            Unmanaged_Card[] cards = BoardToUnmanaged(board);

            CutRange_CheckBet(ref unmanagedRange, (int)actionType, (int)street, cards, betChance, dmContext.GC);
            unmanagedRange.ToManaged(range);
        }


        [DllImport("DecisionMaking.dll", EntryPoint = "CutRange_FoldCallRaise", CharSet = CharSet.Ansi)]
        private static extern void CutRange_FoldCallRaise(ref Unmanaged_Range range, int actionType, int street,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]Unmanaged_Card[] board, float raiseChance, float callChance, IntPtr gc);

        public static void CutRange_FoldCallRaise(Range range, ActionType actionType, Street street, Board board, float raiseChance, float callChance, DecisionMakingContext dmContext)
        {
            Unmanaged_Range unmanagedRange = new Unmanaged_Range(range);
            Unmanaged_Card[] cards = BoardToUnmanaged(board);

            CutRange_FoldCallRaise(ref unmanagedRange, (int)actionType, (int)street, cards, raiseChance, callChance, dmContext.GC);
            unmanagedRange.ToManaged(range);
        }


        [DllImport("DecisionMaking.dll", EntryPoint = "PredictAction_CheckBet", CharSet = CharSet.Ansi)]
        private static extern void PredictAction_CheckBet(ref float toCheck, ref float toBet, ref Unmanaged_Range range, int street,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]Unmanaged_Card[] board, float betChance, IntPtr gc);

        public static void PredictAction_CheckBet(ref float toCheck, ref float toBet, Range range, Street street, Board board, float betChance, DecisionMakingContext dmContext)
        {
            Unmanaged_Range unmanagedRange = new Unmanaged_Range(range);
            Unmanaged_Card[] cards = BoardToUnmanaged(board);

            PredictAction_CheckBet(ref toCheck, ref toBet, ref unmanagedRange, (int)street, cards, betChance, dmContext.GC);
        }


        [DllImport("DecisionMaking.dll", EntryPoint = "PredictAction_FoldCallRaise", CharSet = CharSet.Ansi)]
        private static extern void PredictAction_FoldCallRaise(ref float toFold, ref float toCall, ref float toRaise, ref Unmanaged_Range range, int street,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]Unmanaged_Card[] board, float raiseChance, float callChance, IntPtr gc);

        public static void PredictAction_FoldCallRaise(ref float toFold, ref float toCall, ref float toRaise, Range range,
            Street street, Board board, float raiseChance, float callChance, DecisionMakingContext dmContext)
        {
            Unmanaged_Range unmanagedRange = new Unmanaged_Range(range);
            Unmanaged_Card[] cards = BoardToUnmanaged(board);

            PredictAction_FoldCallRaise(ref toFold, ref toCall, ref toRaise, ref unmanagedRange, (int)street, cards, raiseChance, callChance, dmContext.GC);
        }


        [DllImport("DecisionMaking.dll", EntryPoint = "Range_CutDistribution_CheckBet", CharSet = CharSet.Ansi)]
        private static extern void Range_CutDistribution_CheckBet(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]float[] checkDistribution,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]float[] betDistribution, int numHands, int street, float betChance);

        public static void Range_CutDistribution_CheckBet(float[] checkDistribution, float[] betDistribution, Street street, float betChance)
        {
            Range_CutDistribution_CheckBet(checkDistribution, betDistribution, checkDistribution.Length, (int)street, betChance);
        }


        [DllImport("DecisionMaking.dll", EntryPoint = "Range_CutDistribution_FoldCallRaise", CharSet = CharSet.Ansi)]
        private static extern void Range_CutDistribution_FoldCallRaise(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]float[] foldDistribution,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]float[] callDistribution,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]float[] raiseDistribution, int numHands, int street, float raiseChance, float callChance);

        public static void Range_CutDistribution_FoldCallRaise(float[] foldDistribution, float[] callDistribution, float[] raiseDistribution, Street street, float raiseChance, float callChance)
        {
            Range_CutDistribution_FoldCallRaise(foldDistribution, callDistribution, raiseDistribution, foldDistribution.Length, (int)street, raiseChance, callChance);
        }


        [DllImport("DecisionMaking.dll", EntryPoint = "EstimateEV", CharSet = CharSet.Ansi)]
        private static extern void EstimateEV(ref float checkCallEV, ref float betRaiseEV, int buttonInd, int heroIndex, ref Unmanaged_HoleCards heroHoleCards,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]Unmanaged_Player[] players, int nPlayers,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]Unmanaged_Card[] board,
            int street, int numBets, int numCallers, int bigBlindSize, IntPtr gc);

        public static void Holdem_EstimateEV(out float checkCallEV, out float betRaiseEV, int buttonInd, int heroIndex, HoleCards heroHoleCards,
            List<Player> playerList, Board board, Street street, int numBets, int numCallers, int bigBlindSize, DecisionMakingContext dmContext)
        {
            Unmanaged_HoleCards uHoleCards = new Unmanaged_HoleCards(heroHoleCards);
            Unmanaged_Player[] players = new Unmanaged_Player[playerList.Count];

            for (int i = 0; i < playerList.Count; i++)
            {
                players[i] = new Unmanaged_Player(playerList[i], i);
            }

            Unmanaged_Card[] cards = BoardToUnmanaged(board);

            checkCallEV = 0.0f;
            betRaiseEV = 0.0f;

            EstimateEV(ref checkCallEV, ref betRaiseEV, buttonInd, heroIndex, ref uHoleCards, players, playerList.Count, cards,
                (int)street, numBets, numCallers, bigBlindSize, dmContext.GC);
        }
    }
}
