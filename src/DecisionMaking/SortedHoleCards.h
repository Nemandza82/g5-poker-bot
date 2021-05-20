#pragma once
#include "Common.h"


namespace G5Cpp
{
    struct SortedHoleCards
    {
        int length;
        int ind[N_HOLECARDS];
        float equity[N_HOLECARDS];

        SortedHoleCards()
        {
            length = 0;
        }

        ~SortedHoleCards()
        {
            length = 0;
        }

        SortedHoleCards(int N)
        {
            length = N;
        }

        void clearEquities()
        {
            for (int i = 0; i < N_HOLECARDS; i++)
                equity[i] = 0;
        }

        void setLength(int N)
        {
            length = N;
        }
    };
}
