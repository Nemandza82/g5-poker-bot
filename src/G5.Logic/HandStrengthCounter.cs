using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace G5.Logic
{
    public class HandStrengthCounter
    {
        public const int AHEAD = 1;
        public const int TIE = 0;
        public const int BEHIND = -1;

        private int nCards = 0;

        private int pair0 = 0;
        private int pair1 = 0;
        private int pair2 = 0;
        private int nPairs = 0;

        private int trips0 = 0;
        private int trips1 = 0;
        private int nTrips = 0;

        private int streight0 = 0;
        private int flush = -1;
        private int poker = 0;

        private int[] ranks = new int[16];
        private int[] suits = new int[4];

        private static void InsertSortedHoleCardsToSortedBoard(Card[] allCards, HoleCards heroHoleCards, Card[] board)
        {
            int boardI = 0;
            int handI = 0;
            int k = 0;

            while (k < 7)
            {
                Card heroCard = heroHoleCards.GetCard(handI);

                if (boardI < 5 && handI < 2)
                {
                    if (board[boardI].rank > heroCard.rank)
                    {
                        allCards[k] = board[boardI];
                        boardI++;
                    }
                    else
                    {
                        allCards[k] = heroCard;
                        handI++;
                    }
                }
                else if (boardI < 5)
                {
                    allCards[k] = board[boardI];
                    boardI++;
                }
                else
                {
                    allCards[k] = heroCard;
                    handI++;
                }

                k++;
            }
        }

        /// <summary>
        /// Board and hand are expected to be sorted
        /// </summary>
        private static int GetFlushRank(HoleCards heroHoleCards, Card[] sortedBoard, Card.Suite suite)
        {
            Card[] allCards = new Card[7];

            InsertSortedHoleCardsToSortedBoard(allCards, heroHoleCards, sortedBoard);

            int sum = 0;
            int weight = 15 * 15 * 15 * 15;

            for (int n = 5, k = 0; n > 0; k++)
            {
                if (allCards[k].suite == suite)
                {
                    sum += weight * (int)allCards[k].rank;
                    weight /= 15;
                    n--;
                }
            }

            return sum;
        }

        private HandRank GetHandRank()
        {
            if (poker != 0)
            {
                return HandRank.Poker;
            }
            else if ((nTrips >= 1 && nPairs >= 1) || (nTrips >= 2))
            {
                return HandRank.FullHouse;
            }
            else if (flush >= 0)
            {
                return HandRank.Flush;
            }
            else if (HasStreight())
            {
                return HandRank.Straight;
            }
            else if (nTrips >= 1)
            {
                return HandRank.Trips;
            }
            else if (nPairs >= 2)
            {
                return HandRank.TwoPair;
            }
            else if (nPairs == 1)
            {
                return HandRank.OnePair;
            }
            else
            {
                return HandRank.HighCard;
            }
        }

        private int GetPairRank()
        {
            int maxRank = pair0;
            maxRank = (pair1 > maxRank) ? pair1 : maxRank;
            maxRank = (pair2 > maxRank) ? pair2 : maxRank;
            return maxRank;
        }

        private int GetTwoPairRank()
        {
            if (pair0 < pair1)
            {
                int tmp = pair0;
                pair0 = pair1;
                pair1 = tmp;
            }

            if (pair0 < pair2)
            {
                int tmp = pair0;
                pair0 = pair2;
                pair2 = tmp;
            }

            if (pair1 < pair2)
            {
                int tmp = pair1;
                pair1 = pair2;
                pair2 = tmp;
            }

            return pair0 * 15 + pair1;
        }

        private int GetTripsRank()
        {
            if (trips0 < trips1)
            {
                int tmp = trips0;
                trips0 = trips1;
                trips1 = tmp;
            }

            return trips0;
        }

        private int GetStreightRank()
        {
            return streight0;
        }

        private Card.Suite GetFlushSuite()
        {
            return (Card.Suite)(flush);
        }

        private int GetFullRank()
        {
            int fullRank = 0;

            // Sort trips
            if (trips0 < trips1)
            {
                int tmp = trips0;
                trips0 = trips1;
                trips1 = tmp;
            }

            // Calc rank
            if (nTrips >= 2)
            {
                fullRank = trips0 * 15 + trips1;
            }
            else
            {
                fullRank = trips0 * 15 + GetPairRank();
            }

            return fullRank;
        }

        private int GetPokerRank()
        {
            return poker;
        }

        private int GetHighCardKicker()
        {
            int n = 5;
            int sum = 0;
            int weight = 15 * 15 * 15 * 15;

            for (int i = 14; (n > 0) && (i >= 2); i--)
            {
                if (ranks[i] == 1)
                {
                    sum += weight * i;
                    weight /= 15;
                    n--;
                }
            }

            return sum;
        }

        private int GetOnePairKicker(int pairIndex)
        {
            int n = 3;
            int sum = 0;
            int weight = 15 * 15;

            for (int i = 14; (n > 0) && (i >= 2); i--)
            {
                if ((ranks[i] >= 1) && (i != pairIndex))
                {
                    sum += weight * i;
                    weight /= 15;
                    n--;
                }
            }

            return sum;
        }

        private int GetTwoPairKicker()
        {
            int kicker = 0;

            for (int i = 14; i >= 2; i--)
            {
                if ((ranks[i] >= 1) && (i != pair0) && (i != pair1))
                {
                    kicker = i;
                    break;
                }
            }

            return kicker;
        }

        private int GetTripsKicker()
        {
            int n = 2;
            int sum = 0;
            int weight = 15;

            for (int i = 14; (n > 0) && (i >= 2); i--)
            {
                if ((ranks[i] >= 1) && (i != trips0))
                {
                    sum += weight * i;
                    weight /= 15;
                    n--;
                }
            }

            return sum;
        }

        private int GetPokerKicker()
        {
            int kicker = 0;

            for (int i = 14; i >= 2; i--)
            {
                if (ranks[i] >= 1 && i != poker)
                {
                    kicker = i;
                    break;
                }
            }

            return kicker;
        }

        private bool HasFlush()
        {
            return (flush >= 0);
        }

        private bool HasStreight()
        {
            streight0 = 0;

            {
                int cc = (ranks[14] > 0) ? 1 : 0;

                cc = (ranks[13] > 0) ? (cc + 1) : 0;
                cc = (ranks[12] > 0) ? (cc + 1) : 0;
                cc = (ranks[11] > 0) ? (cc + 1) : 0;
                cc = (ranks[10] > 0) ? (cc + 1) : 0;
                if (cc == 5) { streight0 = 14; return true; }

                cc = (ranks[9] > 0) ? (cc + 1) : 0;
                if (cc == 5) { streight0 = 13; return true; }

                cc = (ranks[8] > 0) ? (cc + 1) : 0;
                if (cc == 5) { streight0 = 12; return true; }

                cc = (ranks[7] > 0) ? (cc + 1) : 0;
                if (cc == 5) { streight0 = 11; return true; }

                cc = (ranks[6] > 0) ? (cc + 1) : 0;
                if (cc == 5) { streight0 = 10; return true; }

                cc = (ranks[5] > 0) ? (cc + 1) : 0;
                if (cc == 5) { streight0 = 9; return true; }

                cc = (ranks[4] > 0) ? (cc + 1) : 0;
                if (cc == 5) { streight0 = 8; return true; }

                cc = (ranks[3] > 0) ? (cc + 1) : 0;
                if (cc == 5) { streight0 = 7; return true; }

                cc = (ranks[2] > 0) ? (cc + 1) : 0;
                if (cc == 5) { streight0 = 6; return true; }

                cc = (ranks[14] > 0) ? (cc + 1) : 0;
                if (cc == 5) { streight0 = 5; return true; }
            }

            return false;
        }

        private void AddPair(int r)
        {
            if (pair0 == 0)
            {
                pair0 = r;
                nPairs++;
            }
            else if (pair1 == 0)
            {
                pair1 = r;
                nPairs++;
            }
            else if (pair2 == 0)
            {
                pair2 = r;
                nPairs++;
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private void RemovePair(int r)
        {
            if (pair0 == r)
            {
                pair0 = 0;
                nPairs--;
            }
            else if (pair1 == r)
            {
                pair1 = 0;
                nPairs--;
            }
            else if (pair2 == r)
            {
                pair2 = 0;
                nPairs--;
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private void AddTrips(int r)
        {
            if (trips0 == 0)
            {
                trips0 = r;
                nTrips++;
            }
            else if (trips1 == 0)
            {
                trips1 = r;
                nTrips++;
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private void RemoveTrips(int r)
        {
            if (trips0 == r)
            {
                trips0 = 0;
                nTrips--;
            }
            else if (trips1 == r)
            {
                trips1 = 0;
                nTrips--;
            }
            else
            {
                Debug.Assert(false);
            }
        }

        public void AddCard(Card card)
        {
            int r = (int)card.rank;
            int s = (int)card.suite;

            ranks[r]++;
            suits[s]++;

            if (suits[s] >= 5)
                flush = s;

            if (ranks[r] == 2)
            {
                AddPair(r);
            }
            else if (ranks[r] == 3)
            {
                AddTrips(r);
                RemovePair(r);
            }
            else if (ranks[r] == 4)
            {
                poker = r;
                RemoveTrips(r);
            }

            nCards++;
        }

        public void RemoveCard(Card card)
        {
            int r = (int)card.rank;
            int s = (int)card.suite;

            ranks[r]--;
            suits[s]--;

            if (suits[s] == 4)
                flush = -1;

            if (ranks[r] == 1)
            {
                RemovePair(r);
            }
            else if (ranks[r] == 2)
            {
                RemoveTrips(r);
                AddPair(r);
            }
            else if (ranks[r] == 3)
            {
                poker = 0;
                AddTrips(r);
            }

            nCards--;
        }

        private int GetHandStrength(HoleCards holeCards, Card[] sortedBoard)
        {
            int strength = 0;

            int base0 = 1;
            int base1 = 15;
            int base2 = base1 * 15;
            int base3 = base2 * 15;
            int base4 = base3 * 15;
            int base5 = base4 * 15;

            bool sFlushFound = false;

            // Check for straight flush
            if (flush >= 0)
            {
                if (HasStreight())
                {
                    int tmpStrength = HandStrength.calculateHandStrength(holeCards, sortedBoard).Value();

                    if (tmpStrength >= ((int)HandRank.SFlush) * base5)
                    {
                        sFlushFound = true;
                        strength = tmpStrength;
                    }
                }
            }

            if (!sFlushFound)
            {
                if (poker != 0)
                {
                    strength = ((int)HandRank.Poker) * base5 +
                                GetPokerRank() * base4 +
                                GetPokerKicker() * base2;
                }
                else if ((nTrips >= 1 && nPairs >= 1) || (nTrips >= 2))
                {
                    strength = ((int)HandRank.FullHouse) * base5 +
                                GetFullRank() * base3;
                }
                else if (flush >= 0)
                {
                    int flushRank = GetFlushRank(holeCards, sortedBoard, (Card.Suite)flush);

                    strength = ((int)HandRank.Flush) * base5 +
                                flushRank * base0;
                }
                else if (HasStreight())
                {
                    strength = ((int)HandRank.Straight) * base5 +
                                GetStreightRank() * base4;
                }
                else if (nTrips >= 1)
                {
                    strength = ((int)HandRank.Trips) * base5 +
                                GetTripsRank() * base4 +
                                GetTripsKicker() * base2;
                }
                else if (nPairs >= 2)
                {
                    strength = ((int)HandRank.TwoPair) * base5 +
                                GetTwoPairRank() * base3 +
                                GetTwoPairKicker() * base2;
                }
                else if (nPairs == 1)
                {
                    int pairRank = GetPairRank();

                    strength = ((int)HandRank.OnePair) * base5 +
                                pairRank * base4 +
                                GetOnePairKicker(pairRank) * base0;
                }
                else
                {
                    strength = ((int)HandRank.HighCard) * base5 +
                                GetHighCardKicker() * base0;
                }
            }

            return strength;
        }

        /// <summary>
        /// Returns AHEAD if ahead, TIE tie, -1 BEHIND.
        /// </summary>
        public int CompareHandStrength(HoleCards heroHoleCards, HoleCards villainHoleCards, HandStrengthCounter villainCounter, Card[] sortedBoard)
        {
            HandRank heroRank = this.GetHandRank();
            HandRank villanRank = villainCounter.GetHandRank();

            bool possibleStraightFlushConflict = false;
            bool resolved = false;
            int result = 0;

            if ((heroRank >= HandRank.Flush && villanRank >= HandRank.Flush))
            {
                if ((this.HasFlush() && this.HasStreight()) || (villainCounter.HasFlush() && villainCounter.HasStreight()))
                {
                    possibleStraightFlushConflict = true;
                }
            }

            if (possibleStraightFlushConflict)
            {
                resolved = false;
            }
            else if (heroRank > villanRank)
            {
                result = AHEAD;
                resolved = true;
            }
            else if (heroRank < villanRank)
            {
                result = BEHIND;
                resolved = true;
            }
            else if (heroRank == HandRank.HighCard)
            {
                int heroTmp = this.GetHighCardKicker();
                int villanTmp = villainCounter.GetHighCardKicker();

                if (heroTmp > villanTmp)
                {
                    result = AHEAD;
                    resolved = true;
                }
                else if (heroTmp < villanTmp)
                {
                    result = BEHIND;
                    resolved = true;
                }
                else
                {
                    result = TIE;
                    resolved = true;
                }
            }
            else if (heroRank == HandRank.OnePair)
            {
                int heroTmp = this.GetPairRank();
                int villanTmp = villainCounter.GetPairRank();

                if (heroTmp > villanTmp)
                {
                    result = AHEAD;
                    resolved = true;
                }
                else if (heroTmp < villanTmp)
                {
                    result = BEHIND;
                    resolved = true;
                }
                else
                {
                    heroTmp = this.GetOnePairKicker(heroTmp);
                    villanTmp = villainCounter.GetOnePairKicker(villanTmp);

                    if (heroTmp > villanTmp)
                    {
                        result = AHEAD;
                        resolved = true;
                    }
                    else if (heroTmp < villanTmp)
                    {
                        result = BEHIND;
                        resolved = true;
                    }
                    else
                    {
                        result = TIE;
                        resolved = true;
                    }
                }
            }
            else if (heroRank == HandRank.TwoPair)
            {
                int heroTmp = this.GetTwoPairRank();
                int villanTmp = villainCounter.GetTwoPairRank();

                if (heroTmp > villanTmp)
                {
                    result = AHEAD;
                    resolved = true;
                }
                else if (heroTmp < villanTmp)
                {
                    result = BEHIND;
                    resolved = true;
                }
                else
                {
                    heroTmp = this.GetTwoPairKicker();
                    villanTmp = villainCounter.GetTwoPairKicker();

                    if (heroTmp > villanTmp)
                    {
                        result = AHEAD;
                        resolved = true;
                    }
                    else if (heroTmp < villanTmp)
                    {
                        result = BEHIND;
                        resolved = true;
                    }
                    else
                    {
                        result = TIE;
                        resolved = true;
                    }
                }
            }
            else if (heroRank == HandRank.Trips)
            {
                int heroTmp = this.GetTripsRank();
                int villanTmp = villainCounter.GetTripsRank();

                if (heroTmp > villanTmp)
                {
                    result = AHEAD;
                    resolved = true;
                }
                else if (heroTmp < villanTmp)
                {
                    result = BEHIND;
                    resolved = true;
                }
                else
                {
                    heroTmp = this.GetTripsKicker();
                    villanTmp = villainCounter.GetTripsKicker();

                    if (heroTmp > villanTmp)
                    {
                        result = AHEAD;
                        resolved = true;
                    }
                    else if (heroTmp < villanTmp)
                    {
                        result = BEHIND;
                        resolved = true;
                    }
                    else
                    {
                        result = TIE;
                        resolved = true;
                    }
                }
            }
            else if (heroRank == HandRank.Straight)
            {
                int heroTmp = this.GetStreightRank();
                int villanTmp = villainCounter.GetStreightRank();

                if (heroTmp > villanTmp)
                {
                    result = AHEAD;
                    resolved = true;
                }
                else if (heroTmp < villanTmp)
                {
                    result = BEHIND;
                    resolved = true;
                }
                else
                {
                    result = TIE;
                    resolved = true;
                }
            }
            else if (heroRank == HandRank.Flush)
            {
                int heroTmp = GetFlushRank(heroHoleCards, sortedBoard, this.GetFlushSuite());
                int villanTmp = GetFlushRank(villainHoleCards, sortedBoard, villainCounter.GetFlushSuite());

                if (heroTmp > villanTmp)
                {
                    result = AHEAD;
                    resolved = true;
                }
                else if (heroTmp < villanTmp)
                {
                    result = BEHIND;
                    resolved = true;
                }
                else
                {
                    result = TIE;
                    resolved = true;
                }
            }
            else if (heroRank == HandRank.FullHouse)
            {
                int heroTmp = this.GetFullRank();
                int villanTmp = villainCounter.GetFullRank();

                if (heroTmp > villanTmp)
                {
                    result = AHEAD;
                    resolved = true;
                }
                else if (heroTmp < villanTmp)
                {
                    result = BEHIND;
                    resolved = true;
                }
                else
                {
                    result = TIE;
                    resolved = true;
                }
            }
            else if (heroRank == HandRank.Poker)
            {
                int heroTmp = this.GetPokerRank();
                int villanTmp = villainCounter.GetPokerRank();

                if (heroTmp > villanTmp)
                {
                    result = AHEAD;
                    resolved = true;
                }
                else if (heroTmp < villanTmp)
                {
                    result = BEHIND;
                    resolved = true;
                }
                else
                {
                    heroTmp = this.GetPokerKicker();
                    villanTmp = villainCounter.GetPokerKicker();

                    if (heroTmp > villanTmp)
                    {
                        result = AHEAD;
                        resolved = true;
                    }
                    else if (heroTmp < villanTmp)
                    {
                        result = BEHIND;
                        resolved = true;
                    }
                    else
                    {
                        result = TIE;
                        resolved = true;
                    }
                }
            }

            if (!resolved)
            {
                villainCounter.GetHandStrength(villainHoleCards, sortedBoard);

                int villanStrength = villainCounter.GetHandStrength(villainHoleCards, sortedBoard);
                int heroStrength = this.GetHandStrength(heroHoleCards, sortedBoard);

                if (heroStrength > villanStrength)
                {
                    result = AHEAD;
                }
                else if (heroStrength < villanStrength)
                {
                    result = BEHIND;
                }
                else
                {
                    result = TIE;
                }
            }

            return result;
        }
    }
}
