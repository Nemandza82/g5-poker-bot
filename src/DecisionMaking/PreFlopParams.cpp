#include "PreFlopParams.h"
#include <assert.h>
#include <stdexcept>
#include <algorithm>


namespace G5Cpp
{
    PreFlopParams::PreFlopParams(TableType tt, Position position, int numCallers, int numRaises, int numPlayers, ActionType previousAction, bool inPositionOnFlop)
    {
        this->tableType = tt;
        this->position = position;
        this->numCallers = numCallers;
        this->numRaises = numRaises;
        this->numPlayers = numPlayers;
        this->previousAction = previousAction;
        this->inPositionOnFlop = inPositionOnFlop;
    }

    bool PreFlopParams::forcedAction() const
    {
        bool forced = true;

        if (position == Position_BigBlind)
        {
            forced = numRaises > 0;
        }

        return forced;
    }

    int PreFlopParams::toIndex() const
    {
        int index = 0;

        if (tableType == TableType_HU)
        {
            if (position == Position_SmallBlind)
            {
                index = std::min(numRaises, 4);
            }
            else if (position == Position_BigBlind)
            {
                index = 5 + std::min(numRaises, 4);
            }
            else
            {
                throw std::runtime_error("Bad position in heads-up game");
            }
        }
        else if (previousAction == Action_Fold) // Prvi krug
        {
            int a0 = (int)position;
            int a1 = -1;
                
            if (numRaises == 0)
            {
                a1 = (numCallers == 0) ? 0 : a1;
                a1 = (numCallers > 0) ? 1 : a1;
            }
            else if (numRaises == 1)
            {
                a1 = (numCallers == 0) ? 2 : a1;
                a1 = (numCallers > 0) ? 3 : a1;
            }
            else if (numRaises >= 2)
            {
                a1 = 4;
            }
 
            assert((a0 != -1) && (a1 != -1));
            index = (5 * a0) + a1; 
        }
        else // Drugi krug. PreviousAction: Check/Call, Raise/Bet
        {
            int a0 = -1;
            int a1 = -1;
            int a2 = -1;
            int a3 = -1;

            a0 = (previousAction == Action_Check || previousAction == Action_Call) ? 0 : a0;
            a0 = (previousAction == Action_Bet || previousAction == Action_Raise) ? 1 : a0;

            a1 = inPositionOnFlop ? 0 : 1;

            a2 = (numPlayers == 2) ? 0 : a2;
            a2 = (numPlayers > 2) ? 1 : a2;

            if (numRaises == 1)
            {
                a3 = (numCallers == 0) ? 0 : a3;
                a3 = (numCallers > 0) ? 1 : a3;
            }
            else if (numRaises >= 2)
            {
                a3 = 2;
            }

            assert((a0 != -1) && (a1 != -1) && (a2 != -1) && (a3 != -1));
            index = 30 + (12 * a0) + (6 * a1) + (3 * a2) + a3;
        }

        return index;
    }
}
