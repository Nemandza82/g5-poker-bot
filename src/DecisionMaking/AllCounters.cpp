#include "AllCounters.h"


namespace G5Cpp
{
    AllCounters::AllCounters()
    {
        _boardLen = 0;

        for (int i=0; i<N_HOLECARDS_DOUBLE; i++)
        {
            _isPreCalculated[i] = false;
        }

        for (int i=0; i<51; i++) // Prva karta u ruci
        {
            for (int j=i+1; j<52; j++) // Druga karta u ruci
            {
                int hcInd = i * 52 + j;
                HoleCards hc(i, j);
                assert (hcInd == hc.toInt());

                _counters[hcInd].addCard(hc.Card0);
                _counters[hcInd].addCard(hc.Card1);
            }
        }
    }

    AllCounters::AllCounters(const AllCounters& copy)
    {
        _boardLen = copy._boardLen;

        for (int i=0; i<N_HOLECARDS_DOUBLE; i++)
        {
            _isPreCalculated[i] = copy._isPreCalculated[i];
        }

        for (int i=0; i<_boardLen; i++)
        {
            _board[i] = copy._board[i];
        }

        for (int i=0; i<51; i++)
        {
            for (int j=i+1; j<52; j++)
            {
                int hcInd = i * 52 + j;
                assert (hcInd == HoleCards(hcInd).toInt());
                
                _preCalculatedHandStrength[hcInd] = copy._preCalculatedHandStrength[hcInd];
                _counters[hcInd] = copy._counters[hcInd];
            }
        }
    }
        
    void AllCounters::addCard(const Card& card)
    {
        _board[_boardLen] = card;
        _boardLen++;

        for (int i=0; i<51; i++)
        {
            for (int j=i+1; j<52; j++)
            {
                int hcInd = i * 52 + j;

                _counters[hcInd].addCard(card);
                _isPreCalculated[hcInd] = false;
            }
        }
    }

    void AllCounters::removeLastCard()
    {
        assert (_boardLen > 0);
        
        _boardLen--;
        Card card = _board[_boardLen];
        
        for (int i=0; i<51; i++)
        {
            for (int j=i+1; j<52; j++)
            {
                int hcInd = i * 52 + j;

                _counters[hcInd].removeCard(card);
                _isPreCalculated[hcInd] = false;
            }
        }
    }

    void AllCounters::calculateAllHandStrengths()
    {
        assert (_boardLen >= 3);

        Card sortedBoard[5];
        sortBoard(sortedBoard, _board, _boardLen);

        for (int i = 0; i < 51; i++) // Prva karta u ruci
        {
            for (int j = i+1; j < 52; j++) // Druga karta u ruci
            {
                int hcInd = i * 52 + j;
                HoleCards holeCards(i, j);
                assert (hcInd == holeCards.toInt());

                _preCalculatedHandStrength[hcInd] = _counters[hcInd].getHandStrength(holeCards, sortedBoard, _boardLen);
                _isPreCalculated[hcInd] = true;
            }
        }
    }
}
