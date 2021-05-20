#pragma once
#include <assert.h>
#include <string>
#include "Common.h"


namespace G5Cpp
{
    struct Card
    {
        enum Suite
        {
            UnknownSuite = -1,
            Clubs = 0,
            Diamonds,
            Hearts,
            Spades
        };

        enum Rank
        {
            UnknownRank = 0,
            Deuce   = 2,
            Three,
            Four,
            Five,
            Six,
            Seven,
            Eight,
            Nine,
            Ten,
            Jack,   // 11
            Queen,  // 12
            King,   // 13
            Ace     // 14
        };

    private:
        Rank _rank;
        Suite _suite;
        int _value;

        void setValue()
        {
            _value = (14 - (int)_rank) * 4 + (int)_suite;
            assert (_value >= 0 && _value <= 51);
        }

    public:

        Card()
        {
            _rank = UnknownRank;
            _suite = UnknownSuite;
            _value = -1;
        }

        Card(int value)
        {
            assert (value >= 0 && value <= 51);

            _suite = (Suite) (value % 4);
            _rank = (Rank) (14 - value / 4);
            _value = value;
        }

        Card(const Card& card)
        {
            _suite = card._suite;
            _rank = card._rank;
            _value = card._value;
        }

        Card(Suite aSuite, Rank aRank)
        {
            _suite = aSuite;
            _rank = aRank;
            setValue();
        }

        Card(const char* stringRepresentation)
        {
            _rank = Card::UnknownRank;
            _suite = Card::UnknownSuite;

            char charRank = stringRepresentation[0];
            char charSuite = stringRepresentation[1];

            if (charRank == '2')
                _rank = Card::Deuce;
            else if (charRank == '3')
                _rank = Card::Three;
            else if (charRank == '4')
                _rank = Card::Four;
            else if (charRank == '5')
                _rank = Card::Five;
            else if (charRank == '6')
                _rank = Card::Six;
            else if (charRank == '7')
                _rank = Card::Seven;
            else if (charRank == '8')
                _rank = Card::Eight;
            else if (charRank == '9')
                _rank = Card::Nine;
            else if (charRank == 'T')
                _rank = Card::Ten;
            else if (charRank == 'J')
                _rank = Card::Jack;
            else if (charRank == 'Q')
                _rank = Card::Queen;
            else if (charRank == 'K')
                _rank = Card::King;
            else if (charRank == 'A')
                _rank = Card::Ace;

            if (charSuite == 'c')
                _suite = Card::Clubs;
            else if (charSuite == 'h')
                _suite = Card::Hearts;
            else if (charSuite == 'd')
                _suite = Card::Diamonds;
            else if (charSuite == 's')
                _suite = Card::Spades;

            setValue();
            assert(_suite != Card::UnknownSuite && _rank != Card::UnknownRank);
        }

        inline Rank rank() const
        {
            return _rank;
        }

        inline Suite suite() const
        {
            return _suite;
        }

        inline int toInt() const
        {
            return _value;
        }
    };

    void sortBoard(Card* sortedBoard, const Card* board, int boardLen);
}
