#pragma once
#include "Common.h"
#include "Card.h"
#include <assert.h>
#include <string>

namespace G5Cpp
{
    struct HoleCards
    {
    private:
        void sortCards()
        {
            if (Card0.toInt() > Card1.toInt())
            {
                Card tmp = Card0;
                Card0 = Card1;
                Card1 = tmp;
            }
        }

    public:

        Card Card0;
        Card Card1;

        HoleCards()
        {
        }

        HoleCards(int index) : Card0(index / 52), Card1(index % 52)
        {
            assert(index >= 0 && index <= 2703);
            sortCards();
        }

        HoleCards(const std::string& str) : Card0(str.substr(0,2).c_str()), Card1(str.substr(2,2).c_str())
        {
            assert(str.length() == 4);
            sortCards();
        }

        HoleCards(const char* str) : Card0(str), Card1(str + 2)
        {
            sortCards();
        }

        HoleCards(Card card0, Card card1) : Card0(card0), Card1(card1)
        {
            sortCards();
        }

        HoleCards(Card* cards) : Card0(cards[0]), Card1(cards[1])
        {
            sortCards();
        }

        HoleCards(int cardIndex0, int cardIndex1) : Card0(cardIndex0), Card1(cardIndex1)
        {
            sortCards();
        }

        HoleCards(Card::Rank rank0, Card::Suite suite0, Card::Rank rank1, Card::Suite suite1) : Card0(suite0, rank0), Card1(suite1, rank1)
        {
            sortCards();
        }

        Card getCard(int ind) const
        {
            return (ind == 0) ? Card0 : Card1;
        }

        inline int toInt() const
        {
            int ind0 = Card0.toInt();
            int ind1 = Card1.toInt();
            return ind0 * 52 + ind1;
        }
    };
}
