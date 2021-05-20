#include "PostFlopParams.h"
#include <assert.h>
#include <algorithm>


namespace G5Cpp
{
    PostFlopParams::PostFlopParams(TableType tt, Street street, int round, ActionType prevStreetAction, int numBets, bool inPosition, int numPlayers)
    {
        this->tableType = tt;
        this->street = street;
        this->round = round;
        this->prevStreetAction = prevStreetAction;
        this->numBets = numBets;
        this->inPosition = inPosition;
        this->numPlayers = numPlayers;
    }

    bool PostFlopParams::forcedAction() const
    {
        return numBets > 0;
    }

    int PostFlopParams::toIndex() const
    {
        int index = 0;
        int prevActionMod = -1;

        prevActionMod = (prevStreetAction == Action_Bet || prevStreetAction == Action_Raise || prevStreetAction == Action_AllIn) ? 0 : prevActionMod;
        prevActionMod = (prevStreetAction == Action_Call) ? 1 : prevActionMod;
        prevActionMod = (prevStreetAction == Action_Check) ? 2 : prevActionMod;

        if (tableType == TableType_HU)
        {
            assert(street == Street_Flop || street == Street_Turn || street == Street_River);

            if (street == Street_Turn)
                index = 15;

            if (street == Street_River)
                index = 30;

            if (inPosition) // SB
            {
                if (numBets < 2)
                {
                    index += (numBets == 0) ? prevActionMod : (3 + prevActionMod);
                }
                else
                {
                    index += (numBets == 2) ? 6 : 7;
                }
            }
            else // BB
            {
                index += 8;
                index += (numBets == 0) ? prevActionMod : (std::min(numBets, 4) + 2);
            }
        }
        else
        {
            int a0 = -1;
            int a1 = -1;

            int a3 = -1;
            int a4 = -1;
            int a5 = -1;

            a0 = (street == Street_Flop) ? 0 : a0;
            a0 = (street == Street_Turn) ? 1 : a0;
            a0 = (street == Street_River) ? 2 : a0;

            a1 = (round == 0) ? 0 : 1;

            a3 = (numBets == 0) ? 0 : a3;
            a3 = (numBets == 1) ? 1 : a3;
            a3 = (numBets >= 2) ? 2 : a3;

            a4 = (inPosition) ? 1 : 0;

            a5 = (numPlayers == 2) ? 0 : a5;
            a5 = (numPlayers >= 3) ? 1 : a5;

            assert((a0 != -1) && (a1 != -1) && (prevActionMod != -1) && (a3 != -1) && (a4 != -1) && (a5 != -1));
            index = a5 + (2 * a4) + (4 * a3) + (12 * prevActionMod) + (36 * a1) + (72 * a0);
        }

        return index;
    }
}
