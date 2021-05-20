#pragma once
#include "Card.h"
#include <vector>
#include <stdexcept>


namespace G5Cpp
{
    struct Board
    {
        Card card[5];
        int size;

        Board(const Card* cardsInBoard, Street street)
        {
            size = 0;

            if (street == Street_Flop)
            {
                size = 3;
            }
            else if (street == Street_Turn)
            {
                size = 4;
            }
            else if (street == Street_River)
            {
                size = 5;
            }

            for (int i = 0; i<size; i++)
            {
                card[i] = cardsInBoard[i];
            }
        }

        void addCard(const Card& cardToAdd)
        {
            assert(size < 5);

            card[size] = cardToAdd;
            size++;
        }

        Street street() const
        {
            if (size < 3)
                return Street_PreFlop;

            if (size == 3)
                return Street_Flop;

            if (size == 4)
                return Street_Turn;

            if (size == 5)
                return Street_River;

            throw std::logic_error("Board in invalid state");
            return Street_Unknown;
        }
    };
}
