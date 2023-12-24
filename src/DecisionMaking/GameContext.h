#pragma once
#include "SortedHoleCards.h"
#include "HoleCards.h"
#include "AllHandStrengths.h"
#include "Board.h"
#include <string>


namespace G5Cpp
{
    class GameContext
    {
        Card _flopCards[3];
        HoleCards _heroHoleCards;

        SortedHoleCards _sortedPreFlop;
        SortedHoleCards _sortedOnFlop;
        SortedHoleCards _sortedOnTurn[52];
        SortedHoleCards _sortedOnRiver[N_HOLECARDS_DOUBLE];

        AllHandStrengths _allHandStrengths;

        void sortHoleCards(float* flopEquities, int* flopCounts, int tStart, int tEnd);
        void sortHoleCards(const Card* flopCards);

    public:

        GameContext(std::string binPath);

        void assertBoard(const Board& board) const;
        void newFlop(const Card* flop, const HoleCards* hc);
        const SortedHoleCards& sortedHoleCards(const Board& board) const;

        inline const AllHandStrengths& allHandStrengths() const
        {
            return _allHandStrengths;
        }
    };
}
