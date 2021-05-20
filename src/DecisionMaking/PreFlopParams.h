#pragma once
#include "Common.h"


namespace G5Cpp
{
    class PreFlopParams
    {
        TableType tableType;
        Position position;
        int numCallers;
        int numRaises;
        int numPlayers;
        ActionType previousAction;
        bool inPositionOnFlop;

    public:
        PreFlopParams(TableType tt, Position position, int numCallers, int numRaises, int numPlayers, ActionType previousAction, bool inPositionOnFlop);

        bool forcedAction() const;
        int toIndex() const;
    };
}
