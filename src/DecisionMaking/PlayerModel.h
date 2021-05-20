#pragma once
#include "Common.h"
#include "PostFlopParams.h"
#include "PreFlopParams.h"
#include <stdexcept>
#include <cassert>


namespace G5Cpp
{
    struct ActionDistribution
    {
        float betRaiseProb;
        float checkCallProb;
        float foldProb;

        ActionDistribution()
        {
            betRaiseProb = 0;
            checkCallProb = 0;
            foldProb = 0;
        }

        void assertNotEmpty()
        {
            assert(betRaiseProb != 0 || checkCallProb != 0 || foldProb != 0);

            if (betRaiseProb == 0 && checkCallProb == 0 && foldProb == 0)
                throw std::runtime_error("Empty ActionDistribution");
        }
    };

    struct PlayerModel
    {
        int totalPlayers;

        ActionDistribution preFlopAD[256];
        ActionDistribution postFlopAD[256];

    public:
        ActionDistribution getAD(const PreFlopParams& preFlopParams) const;
        ActionDistribution getAD(const PostFlopParams& postFlopParams) const;
    };
}
