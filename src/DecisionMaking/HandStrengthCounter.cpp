#include "HandStrengthCounter.h"
#include "HandStrength.h"

namespace G5Cpp
{
    HandStrengthCounter::HandStrengthCounter()
    {
        nCards = 0;

        pair0 = 0;
        pair1 = 0;
        pair2 = 0;
        nPairs = 0;

        trips0 = 0;
        trips1 = 0;
        nTrips = 0;

        flush = -1;
        poker = 0;

        for (int i=0; i<16; i++)
        {
            ranks[i] = 0;
        }

        suits[0] = 0;
        suits[1] = 0;
        suits[2] = 0;
        suits[3] = 0;
    }

    HandRank HandStrengthCounter::getHandRank() const
    {
        if (poker != 0)
        {
            return Rank_Poker;
        }
        else if ((nTrips >= 1 && nPairs >= 1) || (nTrips >= 2))
        {
            return Rank_FullHouse;
        }
        else if (flush >= 0)
        {
            return Rank_Flush;
        }
        else if (getStreightRank() > 0)
        {
            return Rank_Straight;
        }
        else if (nTrips >= 1)
        {
            return Rank_Trips;
        }
        else if (nPairs >= 2)
        {
            return Rank_TwoPair;
        }
        else if (nPairs == 1)
        {
            return Rank_OnePair;
        }
        else
        {
            return Rank_HighCard;
        }
    }

    int HandStrengthCounter::getPairRank() const
    {
        return pair0;
    }

    int HandStrengthCounter::getTwoPairRank() const
    {
        return pair0 * 15 + pair1;
    }

    int HandStrengthCounter::getTripsRank() const 
    {
        return trips0;
    }

    int HandStrengthCounter::getStreightRank() const
    {
        int streight0 = 0;
        {
            int cc = (ranks[14] > 0) ? 1 : 0;

            cc = (ranks[13] > 0) ? (cc + 1) : 0;
            cc = (ranks[12] > 0) ? (cc + 1) : 0;
            cc = (ranks[11] > 0) ? (cc + 1) : 0;
            cc = (ranks[10] > 0) ? (cc + 1) : 0;
            if (cc == 5) { streight0 = 14; return true; }

            cc = (ranks[9] > 0) ? (cc + 1) : 0;
            if (cc == 5) { streight0 = 13; return true; }

            cc = (ranks[8] > 0) ? (cc + 1) : 0;
            if (cc == 5) { streight0 = 12; return true; }

            cc = (ranks[7] > 0) ? (cc + 1) : 0;
            if (cc == 5) { streight0 = 11; return true; }

            cc = (ranks[6] > 0) ? (cc + 1) : 0;
            if (cc == 5) { streight0 = 10; return true; }

            cc = (ranks[5] > 0) ? (cc + 1) : 0;
            if (cc == 5) { streight0 = 9; return true; }

            cc = (ranks[4] > 0) ? (cc + 1) : 0;
            if (cc == 5) { streight0 = 8; return true; }

            cc = (ranks[3] > 0) ? (cc + 1) : 0;
            if (cc == 5) { streight0 = 7; return true; }

            cc = (ranks[2] > 0) ? (cc + 1) : 0;
            if (cc == 5) { streight0 = 6; return true; }

            cc = (ranks[14] > 0) ? (cc + 1) : 0;
            if (cc == 5) { streight0 = 5; return true; }
        }

        return streight0;
    }

    Card::Suite HandStrengthCounter::getFlushSuite() const
    {
        return (Card::Suite) (flush);
    }

    int HandStrengthCounter::getFullRank() const
    {
        int fullRank = 0;

        // Calc rank
        if (nTrips >= 2)
        {
            fullRank = trips0 * 15 + trips1;
        }
        else
        {
            fullRank = trips0 * 15 + pair0;
        }

        return fullRank;
    }

    int HandStrengthCounter::getPokerRank() const
    {
        return poker;
    }

    int HandStrengthCounter::getHighCardKicker() const
    {
        int n = 5;
        int sum = 0;
        int weight = 15 * 15 * 15 * 15;

        for (int i = 14; (n > 0) && (i >= 2); i--)
        {
            if (ranks[i] == 1)
            {
                sum += weight * i;
                weight /= 15;
                n--;
            }
        }

        return sum;
    }

    int HandStrengthCounter::getOnePairKicker(int pairIndex) const
    {
        int n = 3;
        int sum = 0;
        int weight = 15 * 15;

        for (int i = 14; (n > 0) && (i >= 2); i--)
        {
            if ((ranks[i] >= 1) && (i != pairIndex))
            {
                sum += weight * i;
                weight /= 15;
                n--;
            }
        }

        return sum;
    }

    int HandStrengthCounter::getTwoPairKicker() const
    {
        int kicker = 0;

        for (int i = 14; i >= 2; i--)
        {
            if ((ranks[i] >= 1) && (i != pair0) && (i != pair1))
            {
                kicker = i;
                break;
            }
        }

        return kicker;
    }

    int HandStrengthCounter::getTripsKicker() const
    {
        int n = 2;
        int sum = 0;
        int weight = 15;

        for (int i = 14; (n > 0) && (i >= 2); i--)
        {
            if ((ranks[i] >= 1) && (i != trips0))
            {
                sum += weight * i;
                weight /= 15;
                n--;
            }
        }

        return sum;
    }

    int HandStrengthCounter::getPokerKicker() const
    {
        int kicker = 0;

        for (int i = 14; i >= 2; i--)
        {
            if (ranks[i] >= 1 && i != poker)
            {
                kicker = i;
                break;
            }
        }

        return kicker;
    }

    bool HandStrengthCounter::hasFlush() const
    {
        return (flush >= 0);
    }

    void HandStrengthCounter::addPair(int r)
    {
        if (pair0 == 0)
        {
            pair0 = r;
            nPairs++;
        }
        else if (pair1 == 0)
        {
            pair1 = r;
            nPairs++;
        }
        else if (pair2 == 0)
        {
            pair2 = r;
            nPairs++;
        }
        else
        {
            assert(false);
        }

        // Sort two pairs
        if (pair0 < pair1)
        {
            int tmp = pair0;
            pair0 = pair1;
            pair1 = tmp;
        }

        if (pair0 < pair2)
        {
            int tmp = pair0;
            pair0 = pair2;
            pair2 = tmp;
        }

        if (pair1 < pair2)
        {
            int tmp = pair1;
            pair1 = pair2;
            pair2 = tmp;
        }
    }

    void HandStrengthCounter::removePair(int r)
    {
        if (pair0 == r)
        {
            pair0 = 0;
            nPairs--;
        }
        else if (pair1 == r)
        {
            pair1 = 0;
            nPairs--;
        }
        else if (pair2 == r)
        {
            pair2 = 0;
            nPairs--;
        }
        else
        {
            assert(false);
        }

        // Sort two pairs
        if (pair0 < pair1)
        {
            int tmp = pair0;
            pair0 = pair1;
            pair1 = tmp;
        }

        if (pair0 < pair2)
        {
            int tmp = pair0;
            pair0 = pair2;
            pair2 = tmp;
        }

        if (pair1 < pair2)
        {
            int tmp = pair1;
            pair1 = pair2;
            pair2 = tmp;
        }
    }

    void HandStrengthCounter::addTrips(int r)
    {
        if (trips0 == 0)
        {
            trips0 = r;
            nTrips++;
        }
        else if (trips1 == 0)
        {
            trips1 = r;
            nTrips++;
        }
        else
        {
            assert(false);
        }

        if (trips0 < trips1)
        {
            int tmp = trips0;
            trips0 = trips1;
            trips1 = tmp;
        }
    }

    void HandStrengthCounter::removeTrips(int r)
    {
        if (trips0 == r)
        {
            trips0 = 0;
            nTrips--;
        }
        else if (trips1 == r)
        {
            trips1 = 0;
            nTrips--;
        }
        else
        {
            assert(false);
        }

        if (trips0 < trips1)
        {
            int tmp = trips0;
            trips0 = trips1;
            trips1 = tmp;
        }
    }

    void HandStrengthCounter::addCard(const Card& card)
    {
        int r = (int)card.rank();
        int s = (int)card.suite();

        ranks[r]++;
        suits[s]++;

        if (suits[s] >= 5)
            flush = s;

        if (ranks[r] == 2)
        {
            addPair(r);
        }
        else if (ranks[r] == 3)
        {
            addTrips(r);
            removePair(r);
        }
        else if (ranks[r] == 4)
        {
            poker = r;
            removeTrips(r);
        }

        nCards++;
    }

    void HandStrengthCounter::removeCard(const Card& card)
    {
        int r = (int)card.rank();
        int s = (int)card.suite();

        ranks[r]--;
        suits[s]--;

        if (suits[s] == 4)
            flush = -1;

        if (ranks[r] == 1)
        {
            removePair(r);
        }
        else if (ranks[r] == 2)
        {
            removeTrips(r);
            addPair(r);
        }
        else if (ranks[r] == 3)
        {
            poker = 0;
            addTrips(r);
        }

        nCards--;
    }

    int HandStrengthCounter::getHandStrength(const HoleCards& holeCards, const Card* sortedBoard, int boardLen) const
    {
        int strength = 0;

        int base0 = 1;
        int base1 = 15;
        int base2 = base1 * 15;
        int base3 = base2 * 15;
        int base4 = base3 * 15;
        int base5 = base4 * 15;

        bool streightFlushFound = false;

        // Check for straight flush
        if (flush >= 0)
        {
            if (getStreightRank() > 0)
            {
                int tmpStrength = holdem_GetHandStrengthSorted(holeCards, sortedBoard, boardLen);

                if (tmpStrength >= ((int)Rank_StreightFlush) * base5)
                {
                    streightFlushFound = true;
                    strength = tmpStrength;
                }
            }
        }

        if (!streightFlushFound)
        {
            if (poker != 0)
            {
                strength = ((int)Rank_Poker) * base5 +
                            getPokerRank() * base4 +
                            getPokerKicker() * base2;
            }
            else if ((nTrips >= 1 && nPairs >= 1) || (nTrips >= 2))
            {
                strength = ((int)Rank_FullHouse) * base5 +
                            getFullRank() * base3;
            }
            else if (flush >= 0)
            {
                int flushRank = holdem_GetFlushRank(holeCards, sortedBoard, boardLen, (Card::Suite)flush);

                strength = ((int)Rank_Flush) * base5 +
                            flushRank * base0;
            }
            else
            {
                int straight0 = getStreightRank();

                if (straight0 > 0)
                {
                    strength = ((int)Rank_Straight) * base5 +
                                straight0 * base4;
                }
                else if (nTrips >= 1)
                {
                    strength = ((int)Rank_Trips) * base5 +
                                getTripsRank() * base4 +
                                getTripsKicker() * base2;
                }
                else if (nPairs >= 2)
                {
                    strength = ((int)Rank_TwoPair) * base5 +
                                getTwoPairRank() * base3 +
                                getTwoPairKicker() * base2;
                }
                else if (nPairs == 1)
                {
                    int pairRank = getPairRank();

                    strength = ((int)Rank_OnePair) * base5 +
                                pairRank * base4 +
                                getOnePairKicker(pairRank) * base0;
                }
                else
                {
                    strength = ((int)Rank_HighCard) * base5 +
                                getHighCardKicker() * base0;
                }
            }
        }

        return strength;
    }

    /// <summary>
    /// Returns AHEAD if hero is ahead, TIE if there is tie, and BEHIND if hero is behind.
    /// </summary>
    int HandStrengthCounter::compareHandStrength(const HoleCards& heroHoleCards, const HoleCards& villainHoleCards, const HandStrengthCounter& villainCounter, 
        const Card* sortedBoard, int boardLen) const
    {
        HandRank heroRank = getHandRank();
        HandRank villanRank = villainCounter.getHandRank();

        bool possibleStraightFlushConflict = false;
        bool resolved = false;
        int result = 0;

        if ((heroRank >= Rank_Flush && villanRank >= Rank_Flush))
        {
            if ((hasFlush() && getStreightRank() > 0) || (villainCounter.hasFlush() && villainCounter.getStreightRank() > 0))
            {
                possibleStraightFlushConflict = true;
            }
        }

        if (possibleStraightFlushConflict)
        {
            resolved = false;
        }
        else if (heroRank > villanRank)
        {
            result = AHEAD;
            resolved = true;
        }
        else if (heroRank < villanRank)
        {
            result = BEHIND;
            resolved = true;
        }
        else if (heroRank == Rank_HighCard)
        {
            int heroTmp = getHighCardKicker();
            int villanTmp = villainCounter.getHighCardKicker();

            if (heroTmp > villanTmp)
            {
                result = AHEAD;
                resolved = true;
            }
            else if (heroTmp < villanTmp)
            {
                result = BEHIND;
                resolved = true;
            }
            else
            {
                result = TIE;
                resolved = true;
            }
        }
        else if (heroRank == Rank_OnePair)
        {
            int heroTmp = getPairRank();
            int villanTmp = villainCounter.getPairRank();

            if (heroTmp > villanTmp)
            {
                result = AHEAD;
                resolved = true;
            }
            else if (heroTmp < villanTmp)
            {
                result = BEHIND;
                resolved = true;
            }
            else
            {
                heroTmp = getOnePairKicker(heroTmp);
                villanTmp = villainCounter.getOnePairKicker(villanTmp);

                if (heroTmp > villanTmp)
                {
                    result = AHEAD;
                    resolved = true;
                }
                else if (heroTmp < villanTmp)
                {
                    result = BEHIND;
                    resolved = true;
                }
                else
                {
                    result = TIE;
                    resolved = true;
                }
            }
        }
        else if (heroRank == Rank_TwoPair)
        {
            int heroTmp = getTwoPairRank();
            int villanTmp = villainCounter.getTwoPairRank();

            if (heroTmp > villanTmp)
            {
                result = AHEAD;
                resolved = true;
            }
            else if (heroTmp < villanTmp)
            {
                result = BEHIND;
                resolved = true;
            }
            else
            {
                heroTmp = getTwoPairKicker();
                villanTmp = villainCounter.getTwoPairKicker();

                if (heroTmp > villanTmp)
                {
                    result = AHEAD;
                    resolved = true;
                }
                else if (heroTmp < villanTmp)
                {
                    result = BEHIND;
                    resolved = true;
                }
                else
                {
                    result = TIE;
                    resolved = true;
                }
            }
        }
        else if (heroRank == Rank_Trips)
        {
            int heroTmp = getTripsRank();
            int villanTmp = villainCounter.getTripsRank();

            if (heroTmp > villanTmp)
            {
                result = AHEAD;
                resolved = true;
            }
            else if (heroTmp < villanTmp)
            {
                result = BEHIND;
                resolved = true;
            }
            else
            {
                heroTmp = getTripsKicker();
                villanTmp = villainCounter.getTripsKicker();

                if (heroTmp > villanTmp)
                {
                    result = AHEAD;
                    resolved = true;
                }
                else if (heroTmp < villanTmp)
                {
                    result = BEHIND;
                    resolved = true;
                }
                else
                {
                    result = TIE;
                    resolved = true;
                }
            }
        }
        else if (heroRank == Rank_Straight)
        {
            int heroTmp = getStreightRank();
            int villanTmp = villainCounter.getStreightRank();

            if (heroTmp > villanTmp)
            {
                result = AHEAD;
                resolved = true;
            }
            else if (heroTmp < villanTmp)
            {
                result = BEHIND;
                resolved = true;
            }
            else
            {
                result = TIE;
                resolved = true;
            }
        }
        else if (heroRank == Rank_Flush)
        {
            int heroTmp = holdem_GetFlushRank(heroHoleCards, sortedBoard, boardLen, getFlushSuite());
            int villanTmp = holdem_GetFlushRank(villainHoleCards, sortedBoard, boardLen, villainCounter.getFlushSuite());

            if (heroTmp > villanTmp)
            {
                result = AHEAD;
                resolved = true;
            }
            else if (heroTmp < villanTmp)
            {
                result = BEHIND;
                resolved = true;
            }
            else
            {
                result = TIE;
                resolved = true;
            }
        }
        else if (heroRank == Rank_FullHouse)
        {
            int heroTmp = getFullRank();
            int villanTmp = villainCounter.getFullRank();

            if (heroTmp > villanTmp)
            {
                result = AHEAD;
                resolved = true;
            }
            else if (heroTmp < villanTmp)
            {
                result = BEHIND;
                resolved = true;
            }
            else
            {
                result = TIE;
                resolved = true;
            }
        }
        else if (heroRank == Rank_Poker)
        {
            int heroTmp = getPokerRank();
            int villanTmp = villainCounter.getPokerRank();

            if (heroTmp > villanTmp)
            {
                result = AHEAD;
                resolved = true;
            }
            else if (heroTmp < villanTmp)
            {
                result = BEHIND;
                resolved = true;
            }
            else
            {
                heroTmp = getPokerKicker();
                villanTmp = villainCounter.getPokerKicker();

                if (heroTmp > villanTmp)
                {
                    result = AHEAD;
                    resolved = true;
                }
                else if (heroTmp < villanTmp)
                {
                    result = BEHIND;
                    resolved = true;
                }
                else
                {
                    result = TIE;
                    resolved = true;
                }
            }
        }

        if (!resolved)
        {
            int villanStrength = villainCounter.getHandStrength(villainHoleCards, sortedBoard, boardLen);
            int heroStrength = getHandStrength(heroHoleCards, sortedBoard, boardLen);

            if (heroStrength > villanStrength)
            {
                result = AHEAD;
            }
            else if (heroStrength < villanStrength)
            {
                result = BEHIND;
            }
            else
            {
                result = TIE;
            }
        }

        return result;
    }
}
