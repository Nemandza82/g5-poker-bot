#include "Common.h"
#include "Card.h"
#include "Range.h"
#include "SortedHoleCards.h"
#include "AllHandStrengths.h"
#include "GameContext.h"


namespace G5Cpp
{
extern "C"
{
    G5_EXPORT int __stdcall getInt()
    {
        return 5;
    }

    // binPath - Path where dll-s and other binaries are stored. Where to load PreFlopEquities.txt and similar files from.
    G5_EXPORT void* __stdcall CreateGameContext(const char* binPath)
    {
        return new GameContext(binPath);
    }

    G5_EXPORT void __stdcall ReleaseGameContext(void* gc)
    {
        GameContext* gcPtr = static_cast<GameContext*>(gc);
        delete gcPtr;
    }

    G5_EXPORT void __stdcall GameContext_NewFlop(void* gc, const char* strFlop0, const char* strFlop1,
        const char* strFlop2, const char* strHoleCards)
    {
        GameContext* gcPtr = static_cast<GameContext*>(gc);

        if (gcPtr)
        {
            Card flop[3];

            flop[0] = Card(strFlop0);
            flop[1] = Card(strFlop1);
            flop[2] = Card(strFlop2);

            if (strHoleCards)
            {
                HoleCards hc(strHoleCards);
                gcPtr->newFlop(flop, &hc);
            }
            else
            {
                gcPtr->newFlop(flop, 0);
            }
        }
    }

    G5_EXPORT void __stdcall Range_GetSortedHoleCards(Range& range, Street street, const Card* board, const void* gc)
    {
        const GameContext* gcPtr = static_cast<const GameContext*>(gc);
        auto sortedHoleCards = gcPtr->sortedHoleCards(Board(board, street));
        range.fromSortedHoleCards(sortedHoleCards);
    }

    G5_EXPORT void __stdcall CutRange_CheckBet(Range& range, ActionType actionType, Street street, const Card* board,
        float betChance, const void* gc)
    {
        const GameContext* gcPtr = static_cast<const GameContext*>(gc);
        auto res = range.cutCheckBet(actionType, Board(board, street), betChance, *gcPtr);
        range = *res;
    }

    G5_EXPORT void __stdcall CutRange_FoldCallRaise(Range& range, ActionType actionType, Street street, const Card* board,
        float raiseChance, float callChance, const void* gc)
    {
        const GameContext* gcPtr = static_cast<const GameContext*>(gc);
        auto res = range.cutFoldCallRaise(actionType, Board(board, street), raiseChance, callChance, *gcPtr);
        range = *res;
    }

    G5_EXPORT void __stdcall PredictAction_CheckBet(float& toCheck, float& toBet, const Range& range, Street street,
        const Card* board, float betChance, const void* gc)
    {
        const GameContext* gcPtr = static_cast<const GameContext*>(gc);
        range.predictAction_CheckBet(toCheck, toBet, Board(board, street), betChance, *gcPtr);
    }

    G5_EXPORT void __stdcall PredictAction_FoldCallRaise(float& toFold, float& toCall, float& toRaise, const Range& range,
        Street street, const Card* board, float raiseChance, float callChance, const void* gc)
    {
        const GameContext* gcPtr = static_cast<const GameContext*>(gc);
        range.predictAction_FoldCallRaise(toFold, toCall, toRaise, Board(board, street), raiseChance, callChance, *gcPtr);
    }

    G5_EXPORT void __stdcall Range_CutDistribution_CheckBet(float* checkDist, float* betDist, int numHands, Street street, float betChance)
    {
        Range::actionProbDist_CheckBet(checkDist, betDist, numHands, street, betChance);
    }

    G5_EXPORT void __stdcall Range_CutDistribution_FoldCallRaise(float* foldDist, float* callDist, float* raiseDist, int numHands,
        Street street, float raiseChance, float callChance)
    {
        Range::actionProbDist_FoldCallRaise(foldDist, callDist, raiseDist, numHands, raiseChance, callChance);
    }
}
}
