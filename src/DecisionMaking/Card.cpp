#include "Card.h"

namespace G5Cpp
{
    void sortBoard(Card* sortedBoard, const Card* board, int boardLen)
    {
        for (int i=0; i<boardLen; i++)
        {
            sortedBoard[i] = board[i];
        }

        for (int i=0; i<boardLen-1; i++)
        {
            for (int j=i+1; j<boardLen; j++)
            {
                if (sortedBoard[i].rank() < sortedBoard[j].rank())
                {
                    Card tmp = sortedBoard[i];
                    sortedBoard[i] = sortedBoard[j];
                    sortedBoard[j] = tmp;
                }
            }
        }
    }
}
