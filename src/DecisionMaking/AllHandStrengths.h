#pragma once
#include "Common.h"
#include "Card.h"
#include "HoleCards.h"
#include "AllCounters.h"


namespace G5Cpp
{
    class AllHandStrengths
    {
        /// Hand strength for every river card 
        struct RiverHandStrengths
        {
            bool isCalculated;
            int handStrengths[52];
            int sortedRiverCards[52];

            RiverHandStrengths()
            {
                isCalculated = false;
            }
        };

        /// All hand strengths for single turn card
        struct TurnHandStrengths
        {
            bool isCalculated;
            Card turnCard;
            RiverHandStrengths riverHandStrengths[N_HOLECARDS_DOUBLE];
            int currentHandStrength[N_HOLECARDS_DOUBLE];

            TurnHandStrengths()
            {
                isCalculated = false;
            }
        };

        void calcFlopHandStrenghts(int tStart, int tEnd);
        void calcTurnHandStrengths(int turn, const AllCounters& counters, const bool* isBoardCard);
        void calcRiverHandStrengths(int turn, int river, const AllCounters& counters, const bool* isBoardCard);
        void sortRiverHandStrengths(int turn, const bool* isBoardCard);

        Card _board[5];
        int _boardLen;
        HoleCards _heroHoleCards;

        /// All hand strengths for all turn cards
        TurnHandStrengths _turnHandStrengths[52];

    public:

        AllHandStrengths();

        void recalculate(const Card* board, int boardLen, const HoleCards& heroHoleCards);

        inline int getTurnHandStrength(int turn, int holeCardIndex) const
        {
            assert (_turnHandStrengths[turn].isCalculated);
            int handStrength = _turnHandStrengths[turn].currentHandStrength[holeCardIndex];

            assert (handStrength >= 0);
            return handStrength;
        }

        inline const int* getSortedRivers(int turn, int holeCardIndex) const
        {
            assert (_turnHandStrengths[turn].isCalculated);
            return _turnHandStrengths[turn].riverHandStrengths[holeCardIndex].sortedRiverCards;
        }

        inline const int* getRiverHandStrengths(int turn, int holeCardIndex) const
        {
            assert (_turnHandStrengths[turn].isCalculated);
            assert (_turnHandStrengths[turn].riverHandStrengths[holeCardIndex].isCalculated);
            return _turnHandStrengths[turn].riverHandStrengths[holeCardIndex].handStrengths;
        }

        inline int getRiverHandStrength(int turn, int river, int holeCardIndex) const
        {
            assert (_turnHandStrengths[turn].isCalculated);
            assert (_turnHandStrengths[turn].riverHandStrengths[holeCardIndex].isCalculated);

            int handStrength = _turnHandStrengths[turn].riverHandStrengths[holeCardIndex].handStrengths[river];
            assert (handStrength >= 0);

            return handStrength;
        }
    };
}
