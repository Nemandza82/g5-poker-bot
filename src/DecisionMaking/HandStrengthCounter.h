#pragma once
#include "Common.h"
#include "Card.h"
#include "HoleCards.h"

namespace G5Cpp
{
    class HandStrengthCounter
    {
        int nCards;

        int pair0;
        int pair1;
        int pair2;
        int nPairs;

        int trips0;
        int trips1;
        int nTrips;

        int flush;
        int poker;

        int ranks[16];
        int suits[4];

        HandRank getHandRank() const;
        int getPairRank() const;
        int getTwoPairRank() const;
        int getTripsRank() const;
        int getStreightRank() const;
        Card::Suite getFlushSuite() const;
        int getFullRank() const;
        int getPokerRank() const;

        int getHighCardKicker() const;
        int getOnePairKicker(int pairIndex) const;
        int getTwoPairKicker() const;
        int getTripsKicker() const;
        int getPokerKicker() const;

        bool hasFlush() const;

        void addPair(int r);
        void removePair(int r);
        void addTrips(int r);
        void removeTrips(int r);

    public:

        HandStrengthCounter();

        void addCard(const Card& card);
        void removeCard(const Card& card);
        int getHandStrength(const HoleCards& holeCards, const Card* sortedBoard, int boardLen) const;

        /// <summary>
        /// Returns AHEAD if ahead, TIE tie, -1 BEHIND.
        /// </summary>
        int compareHandStrength(const HoleCards& heroHoleCards, const HoleCards& villainHoleCards, const HandStrengthCounter& villainCounter, 
            const Card* sortedBoard, int boardLen) const;
    };
}