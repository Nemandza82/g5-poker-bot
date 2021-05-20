#pragma once
#include "Common.h"
#include "Card.h"
#include "HoleCards.h"

namespace G5Cpp
{
    class HandStrength
    {
    public:

        HandRank rank;
        Card::Rank primaryRank;
        Card::Rank secondRank;
        Card::Rank kicker1Rank;
        Card::Rank kicker2Rank;
        Card::Rank kicker3Rank;

        HandStrength()
        {
            reset();
        }

        HandStrength(const HandStrength& copy)
        {
            rank = copy.rank;
            primaryRank = copy.primaryRank;
            secondRank = copy.secondRank;
            kicker1Rank = copy.kicker1Rank;
            kicker2Rank = copy.kicker2Rank;
            kicker3Rank = copy.kicker3Rank;
        }

        void reset()
        {
            rank = Rank_HighCard;
            primaryRank = Card::UnknownRank;
            secondRank = Card::UnknownRank;
            kicker1Rank = Card::UnknownRank;
            kicker2Rank = Card::UnknownRank;
            kicker3Rank = Card::UnknownRank;
        }

        int value()
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
    };

    int holdem_GetHandStrengthSorted(const HoleCards& heroHoleCards, const Card* sortedBoard, int boardLen);
    int holdem_GetFlushRank(const HoleCards& heroHoleCards, const Card* sortedBoard, int boardLen, Card::Suite suite);
}
