#pragma once
#include "Common.h"
#include "Range.h"
#include "HoleCards.h"
#include "SortedHoleCards.h"


namespace G5Cpp
{
    class PreFlopEquity
    {
        static float _pfeData[N_HOLECARDS_DOUBLE][N_HOLECARDS_DOUBLE];
        static bool _dataLoaded;

    public:

        /**
         * Loads PreFlopEquity-es from hard disk.
         */
        static void load();

        /**
         * Calculates PreFlopEquity of particular HoleCards against an input Range.
         */
        static float calculate(const HoleCards& holeCards, const Range& range);

        /**
         * Gets sorted HoleCards at pre-flop.
         */
        static void getSortedHoleCards(SortedHoleCards& data);
    };
}
