#include "PreFlopEquity.h"
#include "Card.h"
#include "HoleCards.h"
#include <stdio.h>
#include <string>
#include <stdexcept>


namespace G5Cpp
{
    const float BORDER_EQUITY = 0.50f;
    
    bool PreFlopEquity::_dataLoaded = false;
    float PreFlopEquity::_pfeData[N_HOLECARDS_DOUBLE][N_HOLECARDS_DOUBLE];

    void PreFlopEquity::load(std::string binPath)
    {
        if (!_dataLoaded)
        {
            FILE* file = fopen((binPath + "/PreFlopEquities.txt").c_str(), "r");

            for (int i = 0; i < N_HOLECARDS_DOUBLE; i++)
            {
                for (int j = 0; j < N_HOLECARDS_DOUBLE; j++)
                    _pfeData[i][j] = 0.0f;
            }

            char line[256];

            while (fgets(line, sizeof line, file) != 0)
            {
                HoleCards hand1(line + 0);
                HoleCards hand2(line + 5);

                float eq = (float)atof(line + 10);

                _pfeData[hand1.toInt()][hand2.toInt()] = eq / 100.0f;
            }

            fclose(file);
            _dataLoaded = true;
        }
    }

    namespace
    {
        struct EquitySortPair
        {
            int ind;
            float equity;

            EquitySortPair()
            {
                ind = 0;
                equity = 0;
            }

            EquitySortPair(int index, float e)
            {
                ind = index;
                equity = e;
            }

            // Descending compare
            static int compare(const void* a, const void* b)
            {
                EquitySortPair* A = (EquitySortPair*)a;
                EquitySortPair* B = (EquitySortPair*)b;

                if (B->equity < A->equity)
                {
                    return -1;
                }
                else if (B->equity > A->equity)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        };

        /**
         * Calculate Pre-Flop-Equity of particular hole cards against random range.
         */
        float calculateEquity(const float pfe[][N_HOLECARDS_DOUBLE], int handIndex)
        {
            int totalComb = 0;
            float totalEquity = 0;

            for (int i = 0; i < N_HOLECARDS_DOUBLE; i++)
            {
                float eq = pfe[handIndex][i];
                totalEquity += eq * eq;

                if (eq > 0.0f)
                {
                    totalComb++;
                }
            }

            return sqrt(totalEquity / totalComb);
        }

        void sortPreFlopEquities(EquitySortPair* equityList, const float pfe[][N_HOLECARDS_DOUBLE], float borderEquity)
        {
            EquitySortPair initialEquityList[N_HOLECARDS];
        
            for (int i = 0, k = 0; i < 51; i++)
            {
                for (int j = i + 1; j < 52; j++)
                {
                    int ind = i * 52 + j;
                    float eq = calculateEquity(pfe, ind);
                    
                    initialEquityList[k++] = EquitySortPair(ind, eq);
                }
            }

            qsort(initialEquityList, N_HOLECARDS, sizeof(EquitySortPair), EquitySortPair::compare);

            for (int i = 0, k = 0; i < 51; i++)
            {
                for (int j = i + 1; j < 52; j++)
                {
                    int ind = i * 52 + j;

                    float totalEquity = 0;
                    float totalComb = 0;

                    for (int l = 0; l < N_HOLECARDS; l++)
                    {
                        EquitySortPair ep = initialEquityList[l];

                        float pomEq = pfe[ind][ep.ind];

                        if (ep.equity >= borderEquity && pomEq > 0)
                        {
                            totalEquity += pomEq * pomEq;
                            totalComb++;
                        }
                    }

                    equityList[k++] = EquitySortPair(ind, sqrt(totalEquity / totalComb));
                }
            }

            qsort(equityList, N_HOLECARDS, sizeof(EquitySortPair), EquitySortPair::compare);
        }
    } // namespace

    float PreFlopEquity::calculate(const HoleCards& heroHoleCards, const Range& range)
    {
        if (!_dataLoaded)
            throw std::runtime_error("PreFlopEquities not loaded");

        float* heroPFE = &_pfeData[heroHoleCards.toInt()][0];
        
        float totalWeigtht = 0;
        float totalEquity = 0;

        int rangeLength = range.length();
        const int* rangeHCInd = range.hcIndex();
        const float* rangeLikelihood = range.likelihood();

        for (int i = 0; i < rangeLength; i++)
        {
            totalEquity += rangeLikelihood[i] * heroPFE[rangeHCInd[i]];
            totalWeigtht += rangeLikelihood[i];
        }

        return totalEquity / totalWeigtht;
    }

    void PreFlopEquity::getSortedHoleCards(SortedHoleCards& rangeData)
    {
        if (!_dataLoaded)
            throw std::runtime_error("PreFlopEquities not loaded");

        EquitySortPair equityList[N_HOLECARDS];
        sortPreFlopEquities(equityList, _pfeData, BORDER_EQUITY);

        rangeData.length = N_HOLECARDS;

        for (int i=0; i<N_HOLECARDS; i++)
        {
            rangeData.ind[i] = equityList[i].ind;
            rangeData.equity[i] = equityList[i].equity;
        }
    }
}
