#pragma once
#include "Common.h"
#include "Card.h"
#include "SortedHoleCards.h"
#include "GameContext.h"
#include <stdio.h>
#include <stdlib.h>
#include <memory>
#include <cmath>


namespace G5Cpp
{
    class Range
    {
        class ExpSmoothTable
        {
            static const int HASH_LEN = 30;
            float hash[HASH_LEN + 2];

        public:

            ExpSmoothTable()
            {
                float maxExp = 0.98168436111f; // pomExp(1.0f);

                for (int i=0; i<=HASH_LEN; i++)
                {
                    float X = i / (float)HASH_LEN;
                    hash[i] = (1.0f - exp(-(X * 2 * X * 2))) / maxExp;
                }

                hash[HASH_LEN+1] = hash[HASH_LEN];
            }

            inline float doSmooth(float x1, float y1, float x2, float y2, float x)
            {
                float X = 30.0f * (x - x1) / (x2 - x1);
                float Y = y2 - y1;

                int Xi = (int)X;
                float rest = X - Xi;

                return y1 + ((1-rest)*hash[Xi] + rest*hash[Xi+1]) * Y;
            }
        };

        static ExpSmoothTable _smoothTable;

        int _length;
        int _hcInd[N_HOLECARDS];
        float _likelihood[N_HOLECARDS];

        void normalize();
        void cutRange_CalcMultiplier(float* multiplier, const SortedHoleCards& sortedRange, const float* distribution) const;
        void cutRange(const SortedHoleCards& sortedRange, const float* distribution);
        float predictAction(const SortedHoleCards& sortedRange, const float* distribution) const;

    public:

        Range();
        Range(const Range& oldRange);

        void fromSortedHoleCards(const SortedHoleCards& data);

        int length() const
        {
            return _length;
        }

        const int* hcIndex() const
        {
            return _hcInd;
        }

        const float* likelihood() const
        {
            return _likelihood;
        }

        std::shared_ptr<Range> banCard(const Card& card) const;
        std::shared_ptr<Range> cutCheckBet(ActionType actionType, const Board& board, float betChance, const GameContext& gc) const;
        std::shared_ptr<Range> cutFoldCallRaise(ActionType actionType, const Board& board, float raiseChance, float callChance, const GameContext& gc) const;

        void predictAction_CheckBet(float& toCheck, float& toBet, const Board& board, float betChance, const GameContext& gc) const;
        void predictAction_FoldCallRaise(float& toFold, float& toCall, float& toRaise, const Board& board,
            float raiseChance, float callChance, const GameContext& gc) const;

        void fillHandIndices(int* indices, int nIndices) const;

        static void actionProbDist_CheckBet(float* checkDist, float* betDist, int numHands, Street street, float betChance);
        static void actionProbDist_FoldCallRaise(float* foldDist, float* callDist, float* raiseDist, int numHands, float raiseChance, float callChance);
    };
}
