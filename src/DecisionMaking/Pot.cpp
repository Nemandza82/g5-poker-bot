#include "Pot.h"
#include <limits>
#include <algorithm>


namespace G5Cpp
{
    Pot::Pot(const std::vector<Player>& players)
    {
        assert(players.size() <= 6);

        for (int i=0; i<6; i++)
        {
            potHeight[i] = 0;
            moneyInPot[i] = 0;
            maxHandStrength[i] = 0;
            ties[i] = 0;
        }

        int prevHeight = 0;
        bool newHeightExists = true;
        nPots = 0;

        while (newHeightExists)
        {
            int curHeight = std::numeric_limits<int>::max();
            newHeightExists = false;

            for (const Player& player : players)
            {
                if ((player.statusInHand() != Status_Folded) && (player.moneyInPot() > prevHeight))
                {
                    curHeight = std::min(player.moneyInPot(), curHeight);
                    newHeightExists = true;
                }
            }

            if (newHeightExists)
            {
                potHeight[nPots] = curHeight;
                moneyInPot[nPots] = 0;
                maxHandStrength[nPots] = 0;

                for (const Player& player : players)
                {
                    if (player.moneyInPot() > prevHeight)
                        moneyInPot[nPots] += std::min(player.moneyInPot(), curHeight) - prevHeight;
                }

                prevHeight = curHeight;
                nPots++;
            }
        }
    }

    void Pot::addHandStrength(int handStrength, int moneyInThePot)
    {
        for (int i=0; i<nPots; i++)
        {
            if (moneyInThePot >= potHeight[i])
            {
                if (maxHandStrength[i] < handStrength)
                {
                    maxHandStrength[i] = handStrength;
                    ties[i] = 1;
                }
                else if (maxHandStrength[i] == handStrength)
                {
                    ties[i]++;
                }
            }
        }
    }

    float Pot::calculateWinnings(int handStrength, int heroMoneyInThePot)
    {
        float winnings = 0;

        for (int i=0; (i<nPots) && (heroMoneyInThePot>=potHeight[i]); i++)
        {
            if (handStrength >= maxHandStrength[i])
            {
                assert (ties[i] > 0);
                winnings += moneyInPot[i] / (float)ties[i];
            }
        }

        return winnings;
    }

    void Pot::reset()
    {
        for (int i=0; i<nPots; i++)
        {
            maxHandStrength[i] = 0;
            ties[i] = 0;
        }
    }
}
