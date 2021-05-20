#include "HandStrength.h"
#include <assert.h>


namespace G5Cpp
{
    namespace
    {
        bool checkForStraightFlush(HandStrength& handStrength, const Card* cards)
        {
            bool isFlush = (cards[0].suite() == cards[1].suite()) &&
                            (cards[0].suite() == cards[2].suite()) &&
                            (cards[0].suite() == cards[3].suite()) &&
                            (cards[0].suite() == cards[4].suite());

            bool isSFlush = false;

            if (isFlush)
            {
                if (cards[0].rank() - cards[1].rank() == 1 &&
                    cards[1].rank() - cards[2].rank() == 1 &&
                    cards[2].rank() - cards[3].rank() == 1 &&
                    cards[3].rank() - cards[4].rank() == 1)
                {
                    isSFlush = true;
                    handStrength.rank = Rank_StreightFlush;

                    handStrength.primaryRank = cards[0].rank();
                    handStrength.secondRank = Card::UnknownRank;
                    handStrength.kicker1Rank = Card::UnknownRank;
                    handStrength.kicker2Rank = Card::UnknownRank;
                    handStrength.kicker3Rank = Card::UnknownRank;
                }
                else if (cards[0].rank() == Card::Ace &&
                            cards[1].rank() == Card::Five &&
                            cards[2].rank() == Card::Four &&
                            cards[3].rank() == Card::Three &&
                            cards[4].rank() == Card::Deuce)
                {
                    isSFlush = true;
                    handStrength.rank = Rank_StreightFlush;

                    handStrength.primaryRank = Card::Five;
                    handStrength.secondRank = Card::UnknownRank;
                    handStrength.kicker1Rank = Card::UnknownRank;
                    handStrength.kicker2Rank = Card::UnknownRank;
                    handStrength.kicker3Rank = Card::UnknownRank;
                }
            }

            return isSFlush;
        }

        bool checkForPoker(HandStrength& handStrength, const Card* cards)
        {
            bool isPoker = false;

            if (cards[0].rank() == cards[1].rank() &&
                cards[1].rank() == cards[2].rank() &&
                cards[2].rank() == cards[3].rank())
            {
                isPoker = true;
                handStrength.primaryRank = cards[0].rank();
                handStrength.kicker1Rank = cards[4].rank();
            }
            else if (cards[1].rank() == cards[2].rank() &&
                        cards[2].rank() == cards[3].rank() &&
                        cards[3].rank() == cards[4].rank())
            {
                isPoker = true;
                handStrength.primaryRank = cards[1].rank();
                handStrength.kicker1Rank = cards[0].rank();
            }

            if (isPoker)
            {
                handStrength.rank = Rank_Poker;

                handStrength.secondRank = Card::UnknownRank;
                handStrength.kicker2Rank = Card::UnknownRank;
                handStrength.kicker3Rank = Card::UnknownRank;
            }

            return isPoker;
        }

        bool checkForFullHouse(HandStrength& handStrength, const Card* cards)
        {
            bool isFull = false;

            if (cards[0].rank() == cards[1].rank() &&
                cards[1].rank() == cards[2].rank() &&
                cards[3].rank() == cards[4].rank())
            {
                isFull = true;
                handStrength.primaryRank = cards[0].rank();
                handStrength.secondRank = cards[3].rank();
            }

            if (cards[0].rank() == cards[1].rank() &&
                cards[2].rank() == cards[3].rank() &&
                cards[3].rank() == cards[4].rank())
            {
                isFull = true;
                handStrength.primaryRank = cards[2].rank();
                handStrength.secondRank = cards[0].rank();
            }

            if (isFull)
            {
                handStrength.rank = Rank_FullHouse;

                handStrength.kicker1Rank = Card::UnknownRank;
                handStrength.kicker2Rank = Card::UnknownRank;
                handStrength.kicker3Rank = Card::UnknownRank;
            }

            return isFull;
        }

        bool checkForFlush(HandStrength& handStrength, const Card* cards)
        {
            bool isFlush = (cards[0].suite() == cards[1].suite()) &&
                            (cards[0].suite() == cards[2].suite()) &&
                            (cards[0].suite() == cards[3].suite()) &&
                            (cards[0].suite() == cards[4].suite());

            if (isFlush)
            {
                handStrength.rank = Rank_Flush;
                handStrength.primaryRank = cards[0].rank();
                handStrength.secondRank = cards[1].rank();
                handStrength.kicker1Rank = cards[2].rank();
                handStrength.kicker2Rank = cards[3].rank();
                handStrength.kicker3Rank = cards[4].rank();
            }

            return isFlush;
        }

        bool checkForStraight(HandStrength& handStrength, const Card* cards)
        {
            bool isStraight = cards[0].rank() - cards[1].rank() == 1 &&
                                cards[1].rank() - cards[2].rank() == 1 &&
                                cards[2].rank() - cards[3].rank() == 1 &&
                                cards[3].rank() - cards[4].rank() == 1;

            if (cards[0].rank() == Card::Ace &&
                cards[1].rank() == Card::Five &&
                cards[2].rank() == Card::Four &&
                cards[3].rank() == Card::Three &&
                cards[4].rank() == Card::Deuce)
            {
                isStraight = true;
                handStrength.rank = Rank_Straight;

                handStrength.primaryRank = Card::Five;
                handStrength.secondRank = Card::UnknownRank;
                handStrength.kicker1Rank = Card::UnknownRank;
                handStrength.kicker2Rank = Card::UnknownRank;
                handStrength.kicker3Rank = Card::UnknownRank;
            }
            else if (isStraight)
            {
                handStrength.rank = Rank_Straight;

                handStrength.primaryRank = cards[0].rank();
                handStrength.secondRank = Card::UnknownRank;
                handStrength.kicker1Rank = Card::UnknownRank;
                handStrength.kicker2Rank = Card::UnknownRank;
                handStrength.kicker3Rank = Card::UnknownRank;
            }

            return isStraight;
        }

        bool checkForTrips(HandStrength& handStrength, const Card* cards)
        {
            bool isTrips = false;

            if (cards[0].rank() == cards[1].rank() &&
                cards[1].rank() == cards[2].rank())
            {
                isTrips = true;
                handStrength.primaryRank = cards[0].rank();
                handStrength.kicker1Rank = cards[3].rank();
                handStrength.kicker2Rank = cards[4].rank();
            }
            else if (cards[1].rank() == cards[2].rank() &&
                        cards[2].rank() == cards[3].rank())
            {
                isTrips = true;
                handStrength.primaryRank = cards[1].rank();
                handStrength.kicker1Rank = cards[0].rank();
                handStrength.kicker2Rank = cards[4].rank();
            }
            else if (cards[2].rank() == cards[3].rank() &&
                        cards[3].rank() == cards[4].rank())
            {
                isTrips = true;
                handStrength.primaryRank = cards[2].rank();
                handStrength.kicker1Rank = cards[0].rank();
                handStrength.kicker2Rank = cards[1].rank();
            }

            if (isTrips)
            {
                handStrength.rank = Rank_Trips;

                handStrength.secondRank = Card::UnknownRank;
                handStrength.kicker3Rank = Card::UnknownRank;
            }

            return isTrips;
        }

        bool checkForTwoPair(HandStrength& handStrength, const Card* cards)
        {
            bool isTwoPair = false;

            if (cards[0].rank() == cards[1].rank() &&
                cards[2].rank() == cards[3].rank())
            {
                isTwoPair = true;
                handStrength.primaryRank = cards[0].rank();
                handStrength.secondRank = cards[2].rank();
                handStrength.kicker1Rank = cards[4].rank();
            }
            else if (cards[0].rank() == cards[1].rank() &&
                        cards[3].rank() == cards[4].rank())
            {
                isTwoPair = true;
                handStrength.primaryRank = cards[0].rank();
                handStrength.secondRank = cards[3].rank();
                handStrength.kicker1Rank = cards[2].rank();
            }
            else if (cards[1].rank() == cards[2].rank() &&
                        cards[3].rank() == cards[4].rank())
            {
                isTwoPair = true;
                handStrength.primaryRank = cards[1].rank();
                handStrength.secondRank = cards[3].rank();
                handStrength.kicker1Rank = cards[0].rank();
            }

            if (isTwoPair)
            {
                handStrength.rank = Rank_TwoPair;

                handStrength.kicker2Rank = Card::UnknownRank;
                handStrength.kicker3Rank = Card::UnknownRank;
            }

            return isTwoPair;
        }

        bool checkForOnePair(HandStrength& handStrength, const Card* cards)
        {
            bool isOnePair = false;

            if (cards[0].rank() == cards[1].rank())
            {
                isOnePair = true;
                handStrength.primaryRank = cards[0].rank();
                handStrength.kicker1Rank = cards[2].rank();
                handStrength.kicker2Rank = cards[3].rank();
                handStrength.kicker3Rank = cards[4].rank();
            }
            else if (cards[1].rank() == cards[2].rank())
            {
                isOnePair = true;
                handStrength.primaryRank = cards[1].rank();
                handStrength.kicker1Rank = cards[0].rank();
                handStrength.kicker2Rank = cards[3].rank();
                handStrength.kicker3Rank = cards[4].rank();
            }
            else if (cards[2].rank() == cards[3].rank())
            {
                isOnePair = true;
                handStrength.primaryRank = cards[2].rank();
                handStrength.kicker1Rank = cards[0].rank();
                handStrength.kicker2Rank = cards[1].rank();
                handStrength.kicker3Rank = cards[4].rank();
            }
            else if (cards[3].rank() == cards[4].rank())
            {
                isOnePair = true;
                handStrength.primaryRank = cards[3].rank();
                handStrength.kicker1Rank = cards[0].rank();
                handStrength.kicker2Rank = cards[1].rank();
                handStrength.kicker3Rank = cards[2].rank();
            }

            if (isOnePair)
            {
                handStrength.rank = Rank_OnePair;
                handStrength.secondRank = Card::UnknownRank;
            }

            return isOnePair;
        }

        /// <summary>
        /// Will not look bellow hsCandidate.rank()
        /// </summary>
        void getHandStrengthSorted(HandStrength& hsCandidate, const Card* sortedCards)
        {
            checkForStraightFlush(hsCandidate, sortedCards);

            if (hsCandidate.rank > Rank_Poker)
                return;

            checkForPoker(hsCandidate, sortedCards);

            if (hsCandidate.rank > Rank_FullHouse)
                return;

            checkForFullHouse(hsCandidate, sortedCards);

            if (hsCandidate.rank > Rank_Flush)
                return;

            checkForFlush(hsCandidate, sortedCards);

            if (hsCandidate.rank > Rank_Straight)
                return;

            checkForStraight(hsCandidate, sortedCards);

            if (hsCandidate.rank > Rank_Trips)
                return;

            checkForTrips(hsCandidate, sortedCards);

            if (hsCandidate.rank > Rank_TwoPair)
                return;

            checkForTwoPair(hsCandidate, sortedCards);

            if (hsCandidate.rank > Rank_OnePair)
                return;

            checkForOnePair(hsCandidate, sortedCards);

            if (hsCandidate.rank > Rank_HighCard)
                return;

            hsCandidate.primaryRank = sortedCards[0].rank();
            hsCandidate.secondRank = sortedCards[1].rank();
            hsCandidate.kicker1Rank = sortedCards[2].rank();
            hsCandidate.kicker2Rank = sortedCards[3].rank();
            hsCandidate.kicker3Rank = sortedCards[4].rank();
        }

        void insertSortedHoleCardsToSortedBoard(Card* allCards, const HoleCards& heroHoleCards, const Card* board, int boardLen)
        {
            int boardI = 0;
            int handI = 0;
            int k = 0;

            while (k < boardLen + 2)
            {
                Card heroCard = heroHoleCards.getCard(handI);

                if (boardI < boardLen && handI < 2)
                {
                    if (board[boardI].rank() > heroCard.rank())
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
                else if (boardI < boardLen)
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
    } // namespace

    int holdem_GetHandStrengthSorted(const HoleCards& heroHoleCards, const Card* sortedBoard, int boardLen)
    {
        assert (boardLen >= 3);

        Card allCards[7];
        Card possibleHand[5];

        insertSortedHoleCardsToSortedBoard(allCards, heroHoleCards, sortedBoard, boardLen);

        HandStrength hsCandidate;
        HandStrength maxHandStrangth;
        int maxHandStrangthValue = 0;

        if (boardLen == 3)
        {
            getHandStrengthSorted(maxHandStrangth, allCards);
            maxHandStrangthValue = maxHandStrangth.value();
        }
        else if (boardLen == 4)
        {
            for (int i=0; i<6; i++)
            {
                int k = 0;

                if (i != 0)
                    possibleHand[k++] = allCards[0];

                if (i != 1)
                    possibleHand[k++] = allCards[1];

                if (i != 2)
                    possibleHand[k++] = allCards[2];

                if (i != 3)
                    possibleHand[k++] = allCards[3];

                if (i != 4)
                    possibleHand[k++] = allCards[4];

                if (i != 5)
                    possibleHand[k++] = allCards[5];

                getHandStrengthSorted(hsCandidate, possibleHand);
                int value = hsCandidate.value();

                if (maxHandStrangthValue < value)
                {
                    maxHandStrangthValue = value;
                    maxHandStrangth = hsCandidate;
                }
            }
        }
        else // boardLen == 5
        {
            for (int i=0; i<6; i++)
            {
                for (int j=i+1; j<7; j++)
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

                    getHandStrengthSorted(hsCandidate, possibleHand);
                    int value = hsCandidate.value();

                    if (maxHandStrangthValue < value)
                    {
                        maxHandStrangthValue = value;
                        maxHandStrangth = hsCandidate;
                    }
                }
            }
        }

        return maxHandStrangthValue;
    }

    int holdem_GetFlushRank(const HoleCards& heroHoleCards, const Card* sortedBoard, int boardLen, Card::Suite suite)
    {
        assert (boardLen >= 3);

        Card allCards[7];
        insertSortedHoleCardsToSortedBoard(allCards, heroHoleCards, sortedBoard, boardLen);

        int sum = 0;
        int weight = 15 * 15 * 15 * 15;

        for (int n=5, k=0; n>0; k++)
        {
            if (allCards[k].suite() == suite)
            {
                sum += weight * (int)allCards[k].rank();
                weight /= 15;
                n--;
            }
        }

        return sum;
    }
}
