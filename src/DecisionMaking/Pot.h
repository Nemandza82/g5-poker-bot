#pragma once
#include "Player.h"
#include <vector>


namespace G5Cpp
{
    class Pot
    {
        int nPots;
        int potHeight[6];
        int moneyInPot[6];
        int maxHandStrength[6];
        int ties[6];

    public:
        Pot(const std::vector<Player>& players);

        void addHandStrength(int handStrength, int moneyInThePot);
        float calculateWinnings(int handStrength, int heroMoneyInThePot);
        void reset();

        int numPots() const
        {
            return nPots;
        }

        int getHeight(int pot) const
        {
            assert (pot >= 0 && pot < nPots);
            return potHeight[pot];
        }

        int getMoney(int pot) const
        {
            assert (pot >= 0 && pot < nPots);
            return moneyInPot[pot];
        }
    };
}
