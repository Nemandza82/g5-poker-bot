#include "Range.h"
#include <math.h>
#include "HandStrengthCounter.h"
#include "PreFlopEquity.h"
#include <algorithm>
#include <cstring>
#include <mmintrin.h>
#include <emmintrin.h>


namespace G5Cpp
{
    const int NORMALIZE_ITERATIONS = 1; // Default: 8
    const float SMOOTH_DELTA = 0.5f; // Default: 0.5
    const float MIN_CHANCE = 1.0f / (N_HOLECARDS_FLOP * N_HOLECARDS_FLOP);

    Range::ExpSmoothTable Range::_smoothTable = Range::ExpSmoothTable();

    Range::Range()
    {
        // TODO: Note deffault constructor is slow... Not using it...
    }

    Range::Range(const Range& oldRange)
    {
        assert (oldRange._length > 0);
        assert (oldRange._length <= N_HOLECARDS);

        _length = oldRange._length;

        std::memcpy(_hcInd, oldRange._hcInd, _length * sizeof(_hcInd[0]));
        std::memcpy(_likelihood, oldRange._likelihood, _length * sizeof(_likelihood[0]));
    }

    void Range::fromSortedHoleCards(const SortedHoleCards& thatData)
    {
        assert (thatData.length > 0);
        assert (thatData.length <= N_HOLECARDS);

        _length = thatData.length;

        std::memcpy(_hcInd, thatData.ind, _length * sizeof(_hcInd[0]));
        std::memcpy(_likelihood, thatData.equity, _length * sizeof(_likelihood[0]));
    }

    std::shared_ptr<Range> Range::banCard(const Card& card) const
    {
        auto newRange = std::make_shared<Range>(*this);
        int cardInd = card.toInt();

        for (int i = 0; i < _length; i++)
        {
            int c1 = _hcInd[i] / 52;
            int c2 = _hcInd[i] % 52;

            if (c1 == cardInd || c2 == cardInd)
                newRange->_likelihood[i] = 0.0f;
        }

        newRange->normalize();
        return newRange;
    }

    namespace
    {
        union M128_Access
        {
            __m128 v;
            float f[4];
        };

        float M128_getByIndex(__m128 vec, int i)
        {
            if (i >= 0 && i < 4)
            {
                M128_Access access;
                access.v = vec;
                return access.f[i];
            }

            return 0.0f;
        }
    }

    void Range::normalize()
    {
        if (USE_SSE)
        {
            int length4 = (_length / 4) * 4;
            __m128 sum = _mm_set1_ps(0.0f);

            for (int i = 0; i < length4; i+=4)
            {
                sum = _mm_add_ps(sum, _mm_loadu_ps(&_likelihood[i]));
            }

            //float sum_float1 = sum.m128_f32[0] + sum.m128_f32[1] + sum.m128_f32[2] + sum.m128_f32[3];
            float sum_float = M128_getByIndex(sum, 0) + M128_getByIndex(sum, 1) + M128_getByIndex(sum, 2) + M128_getByIndex(sum, 3);

            for (int i = length4; i < _length; i++)
            {
                sum_float += _likelihood[i];
            }

            float norm_float = 1.0f / sum_float;
            __m128 norm = _mm_set1_ps(norm_float);

            for (int i = 0; i < length4; i+=4)
            {
                _mm_storeu_ps(&_likelihood[i], _mm_mul_ps(norm, _mm_loadu_ps(&_likelihood[i])));
            }

            for (int i = length4; i < _length; i++)
            {
                _likelihood[i] *= norm_float;
            }
        }
        else
        {
            float sum = 0;

            for (int i = 0; i < _length; i++)
            {
                sum += _likelihood[i];
            }

            assert (sum != 0);
            float norm = 1 / sum;

            for (int i = 0; i < _length; i++)
            {
                _likelihood[i] *= norm;
            }
        }
    }

    /// Hole cards with same probability get THE SAME multiplier
    void Range::cutRange_CalcMultiplier(float* multiplier, const SortedHoleCards& sortedHoleCards, const float* distribution) const
    {
        float holeCardEquity[N_HOLECARDS_DOUBLE];

        for (int i = 0; i < _length; i++)
        {
            int ind = _hcInd[i];
            holeCardEquity[ind] = _likelihood[i];
            multiplier[ind] = 0.0f;
        }

        float lenf = (float) (sortedHoleCards.length - 1);
        float sumEq = 0.0f;

        for (int i = 0; i < sortedHoleCards.length; )
        {
            float nextSumEq = sumEq;
            int k = i;

            while (k < sortedHoleCards.length && sortedHoleCards.equity[i] == sortedHoleCards.equity[k])
            {
                nextSumEq += holeCardEquity[sortedHoleCards.ind[k]];
                k++;
            }

            int start = (int)(sumEq  * lenf);
            int end = (int)(nextSumEq * lenf);

            float mul = (distribution[start] + distribution[end]) * 0.5f;

            for (int j = i; j < k; j++)
            {
                multiplier[sortedHoleCards.ind[j]] = mul;
            }

            sumEq = nextSumEq;
            i = k;
        }
    }

    void Range::cutRange(const SortedHoleCards& sortedHoleCards, const float* distribution)
    {
        float multiplier[N_HOLECARDS_DOUBLE];
        cutRange_CalcMultiplier(multiplier, sortedHoleCards, distribution);

        for (int i = 0; i < _length; i++)
        {
            int ind = _hcInd[i];
            _likelihood[i] *= multiplier[ind];
        }

        normalize();
    }

    float Range::predictAction(const SortedHoleCards& sortedHoleCards, const float* distribution) const
    {
        float multiplier[N_HOLECARDS_DOUBLE];
        cutRange_CalcMultiplier(multiplier, sortedHoleCards, distribution);

        float sum = 0;

        for (int i = 0; i < _length; i++)
        {
            int ind = _hcInd[i];
            sum += _likelihood[i] * multiplier[ind];
        }

        return sum;
    }

    std::shared_ptr<Range> Range::cutCheckBet(ActionType actionType, const Board& board, float betChance, const GameContext& gc) const
    {
        auto sortedHoleCards = gc.sortedHoleCards(board);

        float checkDistribution[N_HOLECARDS];
        float betDistribution[N_HOLECARDS];

        actionProbDist_CheckBet(checkDistribution, betDistribution, sortedHoleCards.length, board.street(), betChance);
        auto newRange = std::make_shared<Range>(*this);

        if (actionType == Action_Check)
        {
            newRange->cutRange(sortedHoleCards, checkDistribution);
        }
        else if (actionType == Action_Bet || actionType == Action_AllIn)
        {
            newRange->cutRange(sortedHoleCards, betDistribution);
        }
        else
        {
            assert (false);
        }

        return newRange;
    }

    std::shared_ptr<Range> Range::cutFoldCallRaise(ActionType actionType, const Board& board, float raiseChance, float callChance, const GameContext& gc) const
    {
        auto sortedHoleCards = gc.sortedHoleCards(board);

        float foldDistribution[N_HOLECARDS];
        float callDistribution[N_HOLECARDS];
        float raiseDistribution[N_HOLECARDS];

        actionProbDist_FoldCallRaise(foldDistribution, callDistribution, raiseDistribution, sortedHoleCards.length, raiseChance, callChance);
        auto newRange = std::make_shared<Range>(*this);

        if (actionType == Action_Fold)
        {
            newRange->cutRange(sortedHoleCards, foldDistribution);
        }
        else if (actionType == Action_Call)
        {
            newRange->cutRange(sortedHoleCards, callDistribution);
        }
        else if (actionType == Action_Raise || actionType == Action_AllIn)
        {
            newRange->cutRange(sortedHoleCards, raiseDistribution);
        }
        else
        {
            assert (false);
        }

        return newRange;
    }

    void Range::predictAction_CheckBet(float& toCheck, float& toBet, const Board& board, float betChance, const GameContext& gc) const
    {
        auto sortedHoleCards = gc.sortedHoleCards(board);

        float checkDistribution[N_HOLECARDS];
        float betDistribution[N_HOLECARDS];

        actionProbDist_CheckBet(checkDistribution, betDistribution, sortedHoleCards.length, board.street(), betChance);

        toCheck = predictAction(sortedHoleCards, checkDistribution);
        toBet = predictAction(sortedHoleCards, betDistribution);

        float sum = toCheck + toBet;

        toCheck /= sum;
        toBet /= sum;
    }

    void Range::predictAction_FoldCallRaise(float& toFold, float& toCall, float& toRaise, const Board& board,
        float raiseChance, float callChance, const GameContext& gc) const
    {
        auto sortedHoleCards = gc.sortedHoleCards(board);

        float foldDistribution[N_HOLECARDS];
        float callDistribution[N_HOLECARDS];
        float raiseDistribution[N_HOLECARDS];

        actionProbDist_FoldCallRaise(foldDistribution, callDistribution, raiseDistribution, sortedHoleCards.length, raiseChance, callChance);

        toFold = predictAction(sortedHoleCards, foldDistribution);
        toCall = predictAction(sortedHoleCards, callDistribution);
        toRaise = predictAction(sortedHoleCards, raiseDistribution);

        float sum = toFold + toCall + toRaise;

        toFold /= sum;
        toCall /= sum;
        toRaise /= sum;
    }

    void Range::fillHandIndices(int* indices, int nIndices) const
    {
        float cumulHandChance = 0.0f;
        float nextCumulHandChance = 0.0f;
        int lastIndex;

        for (int j = 0; j < _length; j++)
        {
            nextCumulHandChance += _likelihood[j];
            nextCumulHandChance = std::min(nextCumulHandChance, 1.0f);

            int firstIndex = (int)(cumulHandChance * nIndices);
            int nextIndex = (int)(nextCumulHandChance * nIndices);

            if (nextIndex > firstIndex)
            {
                for (int k = firstIndex; k <= nextIndex && k < nIndices; k++)
                {
                    indices[k] = _hcInd[j];
                }

                cumulHandChance = nextCumulHandChance;
                lastIndex = nextIndex;
            }
        }

        assert (lastIndex >= nIndices - 1);
    }

    void Range::actionProbDist_CheckBet(float* checkDist, float* betDist, int numHands, Street street, float betChance)
    {
        float checkArea = 1.0f - betChance;

        if (street == Street_PreFlop)
        {
            float* foldDist = new float[numHands];
            actionProbDist_FoldCallRaise(foldDist, checkDist, betDist, numHands, betChance, checkArea);

            delete [] foldDist;
            return;
        }

        float betA = 0.5f;
        float betB = 0.0001f;

        int betCheckBorder1 = (int)((betChance - std::min(betChance, checkArea) * SMOOTH_DELTA) * numHands);
        int betCheckBorder2 = (int)((betChance + std::min(betChance, checkArea) * SMOOTH_DELTA) * numHands);

        // Bet distribution
        {
            for (int i=0; i<betCheckBorder1; i++)
            {
                betDist[i] = betA;
            }

            for (int i=betCheckBorder1; i<betCheckBorder2; i++)
            {
                betDist[i] = _smoothTable.doSmooth((float)betCheckBorder1, betA, (float)betCheckBorder2, betB, (float)i);
            }

            for (int i=betCheckBorder2; i<numHands; i++)
            {
                betDist[i] = betB;
            }
        }

        float checkA = 0.33f;
        float checkB = 0.67f;

        // Check distribution
        {
            for (int i=0; i<betCheckBorder1; i++)
            {
                checkDist[i] = checkA;
            }

            for (int i=betCheckBorder1; i<betCheckBorder2; i++)
            {
                checkDist[i] = _smoothTable.doSmooth((float)betCheckBorder1, checkA, (float)betCheckBorder2, checkB, (float)i);
            }

            for (int i=betCheckBorder2; i<numHands; i++)
            {
                checkDist[i] = checkB;
            }
        }

        // Normalize
        for (int k=0; k<NORMALIZE_ITERATIONS; k++)
        {
            float sumCheck = 0;
            float sumBet = 0;

            for (int i = 0; i < numHands; i++)
            {
                sumCheck += checkDist[i];
                sumBet += betDist[i];
            }

            float normCheck = (numHands * checkArea) / sumCheck;
            float normBet = (numHands * betChance) / sumBet;

            for (int i = 0; i < numHands; i++)
            {
                float cd = normCheck * checkDist[i];
                float bd = normBet * betDist[i];

                cd = std::max(cd, MIN_CHANCE);
                bd = std::max(bd, MIN_CHANCE);

                checkDist[i] = cd;
                betDist[i] = bd;
            }
        }
    }

    void Range::actionProbDist_FoldCallRaise(float* foldDist, float* callDist, float* raiseDist, int numHands, float raiseChance, float callChance)
    {
        float foldChance = 1.0f - callChance - raiseChance;

        float raiseA = 0.50f;
        float raiseB = 0.0001f;
        float raiseC = 0.0001f;

        float foldA = 0.00f;
        float foldB = 0.0001f;
        float foldC = 0.9998f;

        //float raiseX = raiseA;

        float deltaX =  0;
        deltaX = (deltaX > SMOOTH_DELTA) ? SMOOTH_DELTA : deltaX;

        int slowPlayRaiseBorder = (int)((raiseChance * deltaX) * numHands);
        int raiseCallBorder1 = (int)((raiseChance - std::min(raiseChance, callChance) * SMOOTH_DELTA) * numHands);
        int raiseCallBorder2 = (int)((raiseChance + std::min(callChance, raiseChance) * SMOOTH_DELTA) * numHands);
        int callFoldBorder1 = (int)((raiseChance + callChance - std::min(callChance, foldChance) * SMOOTH_DELTA) * numHands);
        int callFoldBorder2 = (int)((raiseChance + callChance + std::min(foldChance, callChance) * SMOOTH_DELTA) * numHands);

        // Fold distribution
        {
            for (int i=0; i<raiseCallBorder1; i++)
            {
                foldDist[i] = foldA;
            }

            for (int i=raiseCallBorder1; i<raiseCallBorder2; i++)
            {
                foldDist[i] = _smoothTable.doSmooth((float)raiseCallBorder1, foldA, (float)raiseCallBorder2, foldB, (float)i);
            }

            for (int i=raiseCallBorder2; i<callFoldBorder1; i++)
            {
                foldDist[i] = foldB;
            }

            for (int i=callFoldBorder1; i<callFoldBorder2; i++)
            {
                foldDist[i] = _smoothTable.doSmooth((float)callFoldBorder1, foldB, (float)callFoldBorder2, foldC, (float)i);
            }

            for (int i=callFoldBorder2; i<numHands; i++)
            {
                foldDist[i] = foldC;
            }
        }

        // Raise distribution
        {
            for (int i=0; i<slowPlayRaiseBorder; i++)
            {
                raiseDist[i] = _smoothTable.doSmooth(0, raiseA, (float)slowPlayRaiseBorder, raiseA, (float)i);
            }

            for (int i=slowPlayRaiseBorder; i<raiseCallBorder1; i++)
            {
                raiseDist[i] = raiseA;
            }

            for (int i=raiseCallBorder1; i<raiseCallBorder2; i++)
            {
                raiseDist[i] = _smoothTable.doSmooth((float)raiseCallBorder1, raiseA, (float)raiseCallBorder2, raiseB, (float)i);
            }

            for (int i=raiseCallBorder2; i<callFoldBorder1; i++)
            {
                raiseDist[i] = raiseB;
            }

            for (int i=callFoldBorder1; i<callFoldBorder2; i++)
            {
                raiseDist[i] = _smoothTable.doSmooth((float)callFoldBorder1, raiseB, (float)callFoldBorder2, raiseC, (float)i);
            }

            for (int i=callFoldBorder2; i<numHands; i++)
            {
                raiseDist[i] = raiseC;
            }
        }

        // Call distribution
        {
            for (int i=0; i<numHands; i++)
            {
                float tmp = 1.0f - raiseDist[i] - foldDist[i];
                callDist[i] = std::max(tmp, MIN_CHANCE);
            }
        }

        // Normalize
        for (int k=0; k<NORMALIZE_ITERATIONS; k++) // TODO Check num iters
        {
            float sumFold = 0;
            float sumCall = 0;
            float sumRaise = 0;

            for (int i = 0; i < numHands; i++)
            {
                sumFold += foldDist[i];
                sumCall += callDist[i];
                sumRaise += raiseDist[i];
            }

            float normFold = (foldChance * numHands) / sumFold;
            float normCall = (callChance * numHands) / sumCall;
            float normRaise = (raiseChance * numHands) / sumRaise;

            for (int i = 0; i < numHands; i++)
            {
                float fd = normFold * foldDist[i];
                float cd = normCall * callDist[i];
                float rd = normRaise * raiseDist[i];

                fd = std::max(fd, MIN_CHANCE);
                cd = std::max(cd, MIN_CHANCE);
                rd = std::max(rd, MIN_CHANCE);

                foldDist[i] = fd;
                callDist[i] = cd;
                raiseDist[i] = rd;
            }
        }
    }
}
