#pragma once
#include "Common.h"
#include "Card.h"
#include "HoleCards.h"
#include "HandStrengthCounter.h"


namespace G5Cpp
{
    class AllCounters
    {
        HandStrengthCounter _counters[N_HOLECARDS_DOUBLE];
        int _preCalculatedHandStrength[N_HOLECARDS_DOUBLE];
        bool _isPreCalculated[N_HOLECARDS_DOUBLE];

        Card _board[5];
        int _boardLen;

    public:

        AllCounters();
        AllCounters(const AllCounters& copy);
        
        void addCard(const Card& card);
        void removeLastCard();

        void calculateAllHandStrengths();

        const HandStrengthCounter& getCounter(int holeCardIndex) const
        {
            assert (holeCardIndex >= 0);
            assert (holeCardIndex < N_HOLECARDS_DOUBLE);

            return _counters[holeCardIndex];
        }

        inline int getHandStrength(int holeCardIndex) const
        {
            assert (holeCardIndex >= 0);
            assert (holeCardIndex < N_HOLECARDS_DOUBLE);
            assert (_isPreCalculated[holeCardIndex]);
        
            return _preCalculatedHandStrength[holeCardIndex];
        }
    };
}
