using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace G5.Logic
{
    public class Range
    {
        public const int N_HOLECARDS = 52 * 51 / 2;

        /// <summary>
        /// Struktura koja pamti Hole-Cards index i equity koji im je pridruzen.
        /// </summary>
        public struct EquityPair
        {
            public int Ind;
            public float Equity;

            public EquityPair(int index, float e)
            {
                Ind = index;
                Equity = e;
            }

            public override string ToString()
            {
                var i = Ind / 52;
                var j = Ind % 52;

                return (new Card(i)).ToString() + (new Card(j)).ToString() + " - " + (Equity * 100).ToString("f2") + "%";
            }
        };

        public struct CuttingParamsT
        {
            public ActionType ActionType { get; set; }
            public Street Street { get; set; }
            public float Value1 { get; set; }
            public float Value2 { get; set; }
            public bool Forced { get; set; }
        }

        public EquityPair[] Data { get; private set; }
        public List<CuttingParamsT> CuttingParams { get; private set; }
        public List<Card> HeroHoleCards { get; private set; }
        public Board Board { get; private set; }

        public Range()
        {
            Board = new Board();
            HeroHoleCards = new List<Card>();
            CuttingParams = new List<CuttingParamsT>();
            Data = new EquityPair[N_HOLECARDS];

            for (int i = 0, k = 0; i < 51; i++)
            {
                for (var j = i + 1; j < 52; j++)
                {
                    var ind = i * 52 + j;

                    Data[k].Ind = ind;
                    Data[k].Equity = 1.0f;
                    k++;
                }
            }

            Normalize();
        }

        public Range(Range oldRange)
        {
            Data = new EquityPair[N_HOLECARDS];

            for (int i = 0; i < N_HOLECARDS; i++)
            {
                Data[i].Ind = oldRange.Data[i].Ind;
                Data[i].Equity = oldRange.Data[i].Equity;
            }
        }

        public void Reset()
        {
            CuttingParams.Clear();
            HeroHoleCards.Clear();
            Board = new Board();

            for (var i = 0; i < N_HOLECARDS; i++)
            {
                Data[i].Equity = 1.0f;
            }

            Normalize();
        }

        public float getHoleCardsProb(HoleCards holeCards)
        {
            for (var i = 0; i < N_HOLECARDS; i++)
            {
                if (Data[i].Ind == holeCards.ToInt())
                    return Data[i].Equity;
            }

            return 0.0f;
        }

        public void BanCards(List<Card> cards, bool isBoard)
        {
            foreach (var card in cards)
            {
                BanCard(card, isBoard);
            }
        }

        public void BanCard(Card card, bool isBoard)
        {
            if (!isBoard)
            {
                HeroHoleCards.Add(card);
            }
            else
            {
                Board.AddCard(card);
            }

            var cardInd = card.ToInt();

            for (var i = 0; i < N_HOLECARDS; i++)
            {
                var c1 = Data[i].Ind / 52;
                var c2 = Data[i].Ind % 52;

                if (c1 == cardInd || c2 == cardInd)
                    Data[i].Equity = 0.0f;
            }

            Normalize();
        }

        private void Normalize()
        {
            float sum = 0;

            for (var i = 0; i < N_HOLECARDS; i++)
            {
                sum += Data[i].Equity;
            }

            Debug.Assert(sum != 0);
            var norm = (sum != 0) ? (1 / sum) : 1;

            for (var i = 0; i < N_HOLECARDS; i++)
            {
                Data[i].Equity *= norm;
            }
        }

        public void CutCheckBet(ActionType actionType, Street street, Board board, float betChance, DecisionMakingContext dmContext)
        {
            CuttingParams.Add(new CuttingParamsT
            {
                ActionType = actionType,
                Street = street,
                Value1 = betChance,
                Forced = false
            });

            DecisionMakingDll.CutRange_CheckBet(this, actionType, street, board, betChance, dmContext);
        }

        public void CutFoldCallRaise(ActionType actionType, Street street, Board board, float raiseChance, float callChance, DecisionMakingContext dmContext)
        {
            CuttingParams.Add(new CuttingParamsT
            {
                ActionType = actionType,
                Street = street,
                Value1 = raiseChance,
                Value2 = callChance,
                Forced = true
            });

            DecisionMakingDll.CutRange_FoldCallRaise(this, actionType, street, board, raiseChance, callChance, dmContext);
        }
    }
}
