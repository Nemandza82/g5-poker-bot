namespace G5.Logic
{
    /// <summary>
    /// Enumarates all supported poker clients. Eg Ladbrokes, PokerStars. Used for HH parsing..
    /// </summary>
    public enum PokerClient
    {
        Unknown = 0,
        Ladbrokes,
        Poker770,
        PartyPoker,
        PokerStars,
        G5,
        Acpc,
        PokerKing
    }

    /// <summary>
    /// Tip igre (Omaha, Hold'Em ...)
    /// </summary>
    public enum GameType
    {
        Unknown = 0,
        HoldEm,
        Omaha
    }

    // HeadsUp, SixMax
    public enum TableType
    {
        HeadsUp = 2,
        SixMax = 6
    }

    /// <summary>
    /// HighCard, Flush, Poker ...
    /// </summary>
    public enum HandRank
    {
        HighCard = 0,
        OnePair,
        TwoPair,
        Trips,
        Set,
        Straight,
        Flush,
        FullHouse,
        Poker,
        SFlush,
        Count
    };

    /// <summary>
    /// Stanje u kome moze biti igra (Flop, Turn, River...)
    /// </summary>
    public enum Street
    {
        Unknown = 0,
        PreFlop,
        Flop,
        Turn,
        River,
        Count
    };

    /// <summary>
    /// Player position on table: Button, CutOff...
    /// </summary>
    public enum Position
    {
        SmallBlind = 0,
        BigBlind = 1,
        Middle1 = 2,
        Middle2 = 3,
        CutOff = 4,
        Button = 5
    }

    /// <summary>
    /// Moguce akcije igraca: Fold, Call, Check, ukljucujuci Wins, MoneyReturned...
    /// </summary>
    public enum ActionType
    {
        Fold,
        Check,
        Call,
        Bet,
        Raise,
        AllIn,
        Wins,
        MoneyReturned
    };

    public enum OmahaPreFlopStyle
    {
        Agressive,
        Passive
    }

    public enum RaiseAmount
    {
        OneThirdPot,
        HalfPot,
        TwoThirdsPot,
        PotSize
    }

    public enum LimitType
    {
        FL,
        PL,
        NL
    }

    /// <summary>
    /// Status of player in current Hand. Eg. Folded, AllIn...
    /// </summary>
    public enum Status
    {
        ToAct,
        Acted,
        Folded,
        AllIn
    };
}
