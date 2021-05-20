using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace G5.Logic
{
    public class HandStrength
    {
        public HandRank rank = HandRank.HighCard;
        public Card.Rank primaryRank = Card.Rank.Unknown;
        public Card.Rank secondRank = Card.Rank.Unknown;
        public Card.Rank kicker1Rank = Card.Rank.Unknown;
        public Card.Rank kicker2Rank = Card.Rank.Unknown;
        public Card.Rank kicker3Rank = Card.Rank.Unknown;

        public HandStrength()
        {
            Reset();
        }

        public HandStrength(HandStrength copy)
        {
            rank = copy.rank;
            primaryRank = copy.primaryRank;
            secondRank = copy.secondRank;
            kicker1Rank = copy.kicker1Rank;
            kicker2Rank = copy.kicker2Rank;
            kicker3Rank = copy.kicker3Rank;
        }

        public void Reset()
        {
            rank = HandRank.HighCard;
            primaryRank = Card.Rank.Unknown;
            secondRank = Card.Rank.Unknown;
            kicker1Rank = Card.Rank.Unknown;
            kicker2Rank = Card.Rank.Unknown;
            kicker3Rank = Card.Rank.Unknown;
        }

        public int Value()
        {
            int base1 = 15;
            
            int value = ((int)kicker3Rank) +
                        ((int)kicker2Rank) * base1 +
                        ((int)kicker1Rank) * base1 * base1 + 
                        ((int)secondRank) * base1 * base1 * base1 +
                        ((int)primaryRank) * base1 * base1 * base1 * base1 +
                        ((int)rank) * base1 * base1 * base1 * base1 * base1;

            return value;
        }

        override public string ToString()
        {
            string str = rank.ToString();

            str += ": ";

            if (rank == HandRank.HighCard)
                str += Card.RankToString(primaryRank) + ", " +
                       Card.RankToString(secondRank) + ", " +
                       Card.RankToString(kicker1Rank) + ", " +
                       Card.RankToString(kicker2Rank) + ", " +
                       Card.RankToString(kicker3Rank);

            if (rank == HandRank.OnePair)
                str += Card.RankToString(primaryRank) + ", " +
                       Card.RankToString(kicker1Rank) + ", " +
                       Card.RankToString(kicker2Rank) + ", " +
                       Card.RankToString(kicker3Rank);

            if (rank == HandRank.TwoPair)
                str += Card.RankToString(primaryRank) + ", " +
                       Card.RankToString(secondRank) + ", " +
                       Card.RankToString(kicker1Rank);

            if (rank == HandRank.Trips)
                str += Card.RankToString(primaryRank) + ", " +
                       Card.RankToString(kicker1Rank) + ", " +
                       Card.RankToString(kicker2Rank);

            if (rank == HandRank.Straight)
                str += Card.RankToString(primaryRank) + ", " +
                       Card.RankToString(secondRank) + ", " +
                       Card.RankToString(kicker1Rank) + ", " +
                       Card.RankToString(kicker2Rank) + ", " +
                       Card.RankToString(kicker3Rank);

            if (rank == HandRank.Flush)
                str += Card.RankToString(primaryRank) + ", " + 
                       Card.RankToString(secondRank) + ", " +
                       Card.RankToString(kicker1Rank) + ", " +
                       Card.RankToString(kicker2Rank) + ", " +
                       Card.RankToString(kicker3Rank);

            if (rank == HandRank.FullHouse)
                str += Card.RankToString(primaryRank) + ", " +
                       Card.RankToString(secondRank);

            if (rank == HandRank.Poker)
                str += Card.RankToString(primaryRank) + ", " +
                       Card.RankToString(kicker1Rank);

            if (rank == HandRank.SFlush)
                str += Card.RankToString(primaryRank) + ", " +
                       Card.RankToString(secondRank) + ", " +
                       Card.RankToString(kicker1Rank) + ", " +
                       Card.RankToString(kicker2Rank) + ", " +
                       Card.RankToString(kicker3Rank);

            return str;
        }
        
        public static HandStrength calculateHandStrength(HoleCards heroHoleCards, Board board)
        {
            return calculateHandStrength(heroHoleCards, board.getSortedCards());
        }

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
        /// Board and hand are expected to be sorted.
        /// </summary>
        public static HandStrength calculateHandStrength(HoleCards heroHoleCards, Card[] sortedBoard)
        {
            Card[] allCards = new Card[7];
            Card[] possibleHand = new Card[5];

            InsertSortedHoleCardsToSortedBoard(allCards, heroHoleCards, sortedBoard);

            HandStrength hsCandidate = new HandStrength();
            HandStrength maxHandStrangth = null;
            int maxHandStrangthValue = 0;

            for (int i = 0; i < 6; i++)
            {
                for (int j = i + 1; j < 7; j++)
                {
                    int k = 0;

                    if (i != 0 && j != 0)
                        possibleHand[k++] = allCards[0];

                    if (i != 1 && j != 1)
                        possibleHand[k++] = allCards[1];

                    if (i != 2 && j != 2)
                        possibleHand[k++] = allCards[2];

                    if (i != 3 && j != 3)
                        possibleHand[k++] = allCards[3];

                    if (i != 4 && j != 4)
                        possibleHand[k++] = allCards[4];

                    if (i != 5 && j != 5)
                        possibleHand[k++] = allCards[5];

                    if (i != 6 && j != 6)
                        possibleHand[k++] = allCards[6];

                    HandStrength.calculateHandStrengthSorted(ref hsCandidate, possibleHand);
                    int value = hsCandidate.Value();

                    if (maxHandStrangthValue < value)
                    {
                        maxHandStrangthValue = value;
                        maxHandStrangth = new HandStrength(hsCandidate);
                    }
                }
            }

            return maxHandStrangth;
        }

        /// <summary>
        /// Will not look bellow hsCandidate.rank
        /// </summary>
        /// <param name="hsCandidate"></param>
        /// <param name="sortedCards"></param>
        private static void calculateHandStrengthSorted(ref HandStrength hsCandidate, Card[] sortedCards)
        {
            Debug.Assert(sortedCards.Length == 5);

            CheckForStraightFlush(sortedCards, ref hsCandidate);

            if (hsCandidate.rank > HandRank.Poker)
                return;

            CheckForPoker(sortedCards, ref hsCandidate);

            if (hsCandidate.rank > HandRank.FullHouse)
                return;

            CheckForFullHouse(sortedCards, ref hsCandidate);

            if (hsCandidate.rank > HandRank.Flush)
                return;

            CheckForFlush(sortedCards, ref hsCandidate);

            if (hsCandidate.rank > HandRank.Straight)
                return;

            CheckForStraight(sortedCards, ref hsCandidate);

            if (hsCandidate.rank > HandRank.Trips)
                return;

            CheckForTrips(sortedCards, ref hsCandidate);

            if (hsCandidate.rank > HandRank.TwoPair)
                return;

            CheckForTwoPair(sortedCards, ref hsCandidate);

            if (hsCandidate.rank > HandRank.OnePair)
                return;

            CheckForOnePair(sortedCards, ref hsCandidate);

            if (hsCandidate.rank > HandRank.HighCard)
                return;

            hsCandidate.primaryRank = sortedCards[0].rank;
            hsCandidate.secondRank = sortedCards[1].rank;
            hsCandidate.kicker1Rank = sortedCards[2].rank;
            hsCandidate.kicker2Rank = sortedCards[3].rank;
            hsCandidate.kicker3Rank = sortedCards[4].rank;
        }

        private static bool CheckForStraightFlush(Card[] cards, ref HandStrength handStrength)
        {
            Debug.Assert(cards.Length == 5);

            bool isFlush = (cards[0].suite == cards[1].suite) &&
                           (cards[0].suite == cards[2].suite) &&
                           (cards[0].suite == cards[3].suite) &&
                           (cards[0].suite == cards[4].suite);

            bool isSFlush = false;

            if (isFlush)
            {
                if (cards[0].rank - cards[1].rank == 1 &&
                    cards[1].rank - cards[2].rank == 1 &&
                    cards[2].rank - cards[3].rank == 1 &&
                    cards[3].rank - cards[4].rank == 1)
                {
                    isSFlush = true;
                    handStrength.rank = HandRank.SFlush;

                    handStrength.primaryRank = cards[0].rank;
                    handStrength.secondRank = Card.Rank.Unknown;
                    handStrength.kicker1Rank = Card.Rank.Unknown;
                    handStrength.kicker2Rank = Card.Rank.Unknown;
                    handStrength.kicker3Rank = Card.Rank.Unknown;
                }
                else if (cards[0].rank == Card.Rank.Ace &&
                         cards[1].rank == Card.Rank.Five &&
                         cards[2].rank == Card.Rank.Four &&
                         cards[3].rank == Card.Rank.Three &&
                         cards[4].rank == Card.Rank.Deuce)
                {
                    isSFlush = true;
                    handStrength.rank = HandRank.SFlush;

                    handStrength.primaryRank = Card.Rank.Five;
                    handStrength.secondRank = Card.Rank.Unknown;
                    handStrength.kicker1Rank = Card.Rank.Unknown;
                    handStrength.kicker2Rank = Card.Rank.Unknown;
                    handStrength.kicker3Rank = Card.Rank.Unknown;
                }
            }

            return isSFlush;
        }

        private static bool CheckForPoker(Card[] cards, ref HandStrength handStrength)
        {
            Debug.Assert(cards.Length == 5);
            bool isPoker = false;

            if (cards[0].rank == cards[1].rank &&
                cards[1].rank == cards[2].rank &&
                cards[2].rank == cards[3].rank)
            {
                isPoker = true;
                handStrength.primaryRank = cards[0].rank;
                handStrength.kicker1Rank = cards[4].rank;
            }
            else if (cards[1].rank == cards[2].rank &&
                     cards[2].rank == cards[3].rank &&
                     cards[3].rank == cards[4].rank)
            {
                isPoker = true;
                handStrength.primaryRank = cards[1].rank;
                handStrength.kicker1Rank = cards[0].rank;
            }

            if (isPoker)
            {
                handStrength.rank = HandRank.Poker;

                handStrength.secondRank = Card.Rank.Unknown;
                handStrength.kicker2Rank = Card.Rank.Unknown;
                handStrength.kicker3Rank = Card.Rank.Unknown;
            }

            return isPoker;
        }

        private static bool CheckForFullHouse(Card[] cards, ref HandStrength handStrength)
        {
            Debug.Assert(cards.Length == 5);
            bool isFull = false;

            if (cards[0].rank == cards[1].rank &&
                cards[1].rank == cards[2].rank &&
                cards[3].rank == cards[4].rank)
            {
                isFull = true;
                handStrength.primaryRank = cards[0].rank;
                handStrength.secondRank = cards[3].rank;
            }

            if (cards[0].rank == cards[1].rank &&
                cards[2].rank == cards[3].rank &&
                cards[3].rank == cards[4].rank)
            {
                isFull = true;
                handStrength.primaryRank = cards[2].rank;
                handStrength.secondRank = cards[0].rank;
            }

            if (isFull)
            {
                handStrength.rank = HandRank.FullHouse;

                handStrength.kicker1Rank = Card.Rank.Unknown;
                handStrength.kicker2Rank = Card.Rank.Unknown;
                handStrength.kicker3Rank = Card.Rank.Unknown;
            }

            return isFull;
        }

        private static bool CheckForFlush(Card[] cards, ref HandStrength handStrength)
        {
            Debug.Assert(cards.Length == 5);

            bool isFlush = (cards[0].suite == cards[1].suite) &&
                           (cards[0].suite == cards[2].suite) &&
                           (cards[0].suite == cards[3].suite) &&
                           (cards[0].suite == cards[4].suite);

            if (isFlush)
            {
                handStrength.rank = HandRank.Flush;
                handStrength.primaryRank = cards[0].rank;
                handStrength.secondRank = cards[1].rank;
                handStrength.kicker1Rank = cards[2].rank;
                handStrength.kicker2Rank = cards[3].rank;
                handStrength.kicker3Rank = cards[4].rank;
            }

            return isFlush;
        }

        private static bool CheckForStraight(Card[] cards, ref HandStrength handStrength)
        {
            Debug.Assert(cards.Length == 5);

            bool isStraight = cards[0].rank - cards[1].rank == 1 &&
                              cards[1].rank - cards[2].rank == 1 &&
                              cards[2].rank - cards[3].rank == 1 &&
                              cards[3].rank - cards[4].rank == 1;

            if (cards[0].rank == Card.Rank.Ace &&
                cards[1].rank == Card.Rank.Five &&
                cards[2].rank == Card.Rank.Four &&
                cards[3].rank == Card.Rank.Three &&
                cards[4].rank == Card.Rank.Deuce)
            {
                isStraight = true;
                handStrength.rank = HandRank.Straight;

                handStrength.primaryRank = Card.Rank.Five;
                handStrength.secondRank = Card.Rank.Unknown;
                handStrength.kicker1Rank = Card.Rank.Unknown;
                handStrength.kicker2Rank = Card.Rank.Unknown;
                handStrength.kicker3Rank = Card.Rank.Unknown;
            }
            else if (isStraight)
            {
                handStrength.rank = HandRank.Straight;

                handStrength.primaryRank = cards[0].rank;
                handStrength.secondRank = Card.Rank.Unknown;
                handStrength.kicker1Rank = Card.Rank.Unknown;
                handStrength.kicker2Rank = Card.Rank.Unknown;
                handStrength.kicker3Rank = Card.Rank.Unknown;
            }

            return isStraight;
        }

        private static bool CheckForTrips(Card[] cards, ref HandStrength handStrength)
        {
            Debug.Assert(cards.Length == 5);
            bool isTrips = false;

            if (cards[0].rank == cards[1].rank &&
                cards[1].rank == cards[2].rank)
            {
                isTrips = true;
                handStrength.primaryRank = cards[0].rank;
                handStrength.kicker1Rank = cards[3].rank;
                handStrength.kicker2Rank = cards[4].rank;
            }
            else if (cards[1].rank == cards[2].rank &&
                     cards[2].rank == cards[3].rank)
            {
                isTrips = true;
                handStrength.primaryRank = cards[1].rank;
                handStrength.kicker1Rank = cards[0].rank;
                handStrength.kicker2Rank = cards[4].rank;
            }
            else if (cards[2].rank == cards[3].rank &&
                     cards[3].rank == cards[4].rank)
            {
                isTrips = true;
                handStrength.primaryRank = cards[2].rank;
                handStrength.kicker1Rank = cards[0].rank;
                handStrength.kicker2Rank = cards[1].rank;
            }

            if (isTrips)
            {
                handStrength.rank = HandRank.Trips;

                handStrength.secondRank = Card.Rank.Unknown;
                handStrength.kicker3Rank = Card.Rank.Unknown;
            }

            return isTrips;
        }

        private static bool CheckForTwoPair(Card[] cards, ref HandStrength handStrength)
        {
            Debug.Assert(cards.Length == 5);
            bool isTwoPair = false;

            if (cards[0].rank == cards[1].rank &&
                cards[2].rank == cards[3].rank)
            {
                isTwoPair = true;
                handStrength.primaryRank = cards[0].rank;
                handStrength.secondRank = cards[2].rank;
                handStrength.kicker1Rank = cards[4].rank;
            }
            else if (cards[0].rank == cards[1].rank &&
                     cards[3].rank == cards[4].rank)
            {
                isTwoPair = true;
                handStrength.primaryRank = cards[0].rank;
                handStrength.secondRank = cards[3].rank;
                handStrength.kicker1Rank = cards[2].rank;
            }
            else if (cards[1].rank == cards[2].rank &&
                     cards[3].rank == cards[4].rank)
            {
                isTwoPair = true;
                handStrength.primaryRank = cards[1].rank;
                handStrength.secondRank = cards[3].rank;
                handStrength.kicker1Rank = cards[0].rank;
            }

            if (isTwoPair)
            {
                handStrength.rank = HandRank.TwoPair;

                handStrength.kicker2Rank = Card.Rank.Unknown;
                handStrength.kicker3Rank = Card.Rank.Unknown;
            }

            return isTwoPair;
        }

        private static bool CheckForOnePair(Card[] cards, ref HandStrength handStrength)
        {
            Debug.Assert(cards.Length == 5);
            bool isOnePair = false;

            if (cards[0].rank == cards[1].rank)
            {
                isOnePair = true;
                handStrength.primaryRank = cards[0].rank;
                handStrength.kicker1Rank = cards[2].rank;
                handStrength.kicker2Rank = cards[3].rank;
                handStrength.kicker3Rank = cards[4].rank;
            }
            else if (cards[1].rank == cards[2].rank)
            {
                isOnePair = true;
                handStrength.primaryRank = cards[1].rank;
                handStrength.kicker1Rank = cards[0].rank;
                handStrength.kicker2Rank = cards[3].rank;
                handStrength.kicker3Rank = cards[4].rank;
            }
            else if (cards[2].rank == cards[3].rank)
            {
                isOnePair = true;
                handStrength.primaryRank = cards[2].rank;
                handStrength.kicker1Rank = cards[0].rank;
                handStrength.kicker2Rank = cards[1].rank;
                handStrength.kicker3Rank = cards[4].rank;
            }
            else if (cards[3].rank == cards[4].rank)
            {
                isOnePair = true;
                handStrength.primaryRank = cards[3].rank;
                handStrength.kicker1Rank = cards[0].rank;
                handStrength.kicker2Rank = cards[1].rank;
                handStrength.kicker3Rank = cards[2].rank;
            }

            if (isOnePair)
            {
                handStrength.rank = HandRank.OnePair;

                handStrength.secondRank = Card.Rank.Unknown;
            }

            return isOnePair;
        }
    }
}
