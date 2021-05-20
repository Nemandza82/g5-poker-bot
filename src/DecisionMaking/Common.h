#pragma once


#if defined(_MSC_VER)
    #define G5_EXPORT __declspec(dllexport)
#elif defined(__GNUC__)
    #define G5_EXPORT __attribute__((visibility("default")))
    #define __stdcall
#endif


namespace G5Cpp
{
    extern "C" G5_EXPORT int __stdcall getInt();
    extern "C" G5_EXPORT void* __stdcall CreateGameContext();
    extern "C" G5_EXPORT void __stdcall ReleaseGameContext(void* gc);

    const int N_HOLECARDS_DOUBLE = 51 * 52 + 52;
    const int N_HOLECARDS = 52 * 51 / 2;
    const int N_HOLECARDS_FLOP = 49 * 48 / 2;
    const int N_HOLECARDS_TURN = 48 * 47 / 2;
    const int N_HOLECARDS_RIVER = 47 * 46 / 2;

    const bool USE_MT = true;
    const bool USE_SSE = true;

    static const int AHEAD = 1;
    static const int TIE = 0;
    static const int BEHIND = -1;

    enum Street
    {
        Street_Unknown = 0,
        Street_PreFlop,
        Street_Flop,
        Street_Turn,
        Street_River
    };

    enum Position
    {
        Position_SmallBlind = 0,
        Position_BigBlind = 1,
        Position_Middle1 = 2,
        Position_Middle2 = 3,
        Position_CutOff = 4,
        Position_Button = 5
    };

    enum TableType
    {
        TableType_HU = 2,
        TableType_SixMax = 6
    };

    enum HandRank
    {
        Rank_HighCard = 0,
        Rank_OnePair,
        Rank_TwoPair,
        Rank_Trips,
        Rank_Set,
        Rank_Straight,
        Rank_Flush,
        Rank_FullHouse,
        Rank_Poker,
        Rank_StreightFlush,
        Rank_Length
    };

    /// <summary>
    /// Status of player in current Hand. Eg. Folded, AllIn...
    /// </summary>
    enum Status
    {
        Status_ToAct,
        Status_Acted,
        Status_Folded,
        Status_WentAllIn
    };

    /// <summary>
    /// Moguce akcije igraca: Fold, Call, Check, ukljucujuci Wins, MoneyReturned...
    /// </summary>
    enum ActionType
    {
        Action_Fold,
        Action_Check,
        Action_Call,
        Action_Bet,
        Action_Raise,
        Action_AllIn,
        Action_Wins,
        Action_MoneyReturned
    };

    int divUp(int val, int mul);
    int alignUp(int val, int mul);
}
