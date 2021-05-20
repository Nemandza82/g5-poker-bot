#include "AllHandStrengths.h"
#include "tbb/parallel_for.h"
#include "tbb/blocked_range.h"


namespace G5Cpp
{
    void AllHandStrengths::calcTurnHandStrengths(int turn, const AllCounters& counters, const bool* isBoardCard)
    {
        TurnHandStrengths* ths = &_turnHandStrengths[turn];
        ths->isCalculated = true;
        ths->turnCard = Card(turn);

        for (int i = 0; i < N_HOLECARDS_DOUBLE; i++)
        {
            ths->currentHandStrength[i] = -1;
            ths->riverHandStrengths[i].isCalculated = false;

            for (int j = 0; j < 52; j++)
            {
                ths->riverHandStrengths[i].handStrengths[j] = -1;
                ths->riverHandStrengths[i].sortedRiverCards[j] = -1;
            }
        }

        for (int i = 0; i < 51; i++) // Prva karta u ruci
        {
            if (isBoardCard[i])
                continue;

            for (int j = i+1; j < 52; j++) // Druga karta u ruci
            {
                if (isBoardCard[j])
                    continue;

                int hcInd = i * 52 + j;
                ths->currentHandStrength[hcInd] = counters.getHandStrength(hcInd);
            }
        }
    }

    void AllHandStrengths::calcRiverHandStrengths(int turn, int river, const AllCounters& counters, const bool* isBoardCard)
    {
        TurnHandStrengths* ths = &_turnHandStrengths[turn];

        for (int i = 0; i < 51; i++) // Prva karta u ruci
        {
            if (isBoardCard[i])
                continue;

            for (int j = i+1; j < 52; j++) // Druga karta u ruci
            {
                if (isBoardCard[j])
                    continue;

                int hcInd = i * 52 + j;
                ths->riverHandStrengths[hcInd].isCalculated = true;
                ths->riverHandStrengths[hcInd].handStrengths[river] = counters.getHandStrength(hcInd);
            }
        }
    }

    namespace
    {
        struct StrengthSortPair
        {
            int ind;
            int strength;

            StrengthSortPair()
            {
                ind = 0;
                strength = 0;
            }

            StrengthSortPair(int index, int s)
            {
                ind = index;
                strength = s;
            }

            // Descending compare
            static int Compare(const void* a, const void* b)
            {
                StrengthSortPair* A = (StrengthSortPair*)a;
                StrengthSortPair* B = (StrengthSortPair*)b;

                return (B->strength - A->strength);
            }
        };
    } // namespace

    void AllHandStrengths::sortRiverHandStrengths(int turn, const bool* isBoardCard)
    {
        TurnHandStrengths* ths = &_turnHandStrengths[turn];

        for (int i = 0; i < 51; i++) // Prva karta u ruci
        {
            if (isBoardCard[i])
                continue;

            for (int j = i+1; j < 52; j++) // Druga karta u ruci
            {
                if (isBoardCard[j])
                    continue;

                int hcInd = i * 52 + j;
                StrengthSortPair pairs[52];

                for (int k = 0; k < 52; k++)
                {
                    pairs[k].ind = k;
                    pairs[k].strength = ths->riverHandStrengths[hcInd].handStrengths[k];
                }

                qsort(pairs, 52, sizeof(StrengthSortPair), StrengthSortPair::Compare);

                for (int k = 0; k < 52; k++)
                {
                    ths->riverHandStrengths[hcInd].sortedRiverCards[k] = pairs[k].ind;
                }
            }
        }
    }

    void AllHandStrengths::calcFlopHandStrenghts(int tStart, int tEnd)
    {
        AllCounters counters;
        counters.addCard(_board[0]);
        counters.addCard(_board[1]);
        counters.addCard(_board[2]);

        bool isHeroCard[52];
        bool isBoardCard[52];

        for (int i=0; i<52; i++)
        {
            isHeroCard[i] = false;
            isBoardCard[i] = false;
        }

        isBoardCard[_board[0].toInt()] = true;
        isBoardCard[_board[1].toInt()] = true;
        isBoardCard[_board[2].toInt()] = true;

        isHeroCard[_heroHoleCards.Card0.toInt()] = true;
        isHeroCard[_heroHoleCards.Card1.toInt()] = true;

        // Letting the turn go
        for (int i = tStart; i < tEnd; i++)
        {
            if (isBoardCard[i] || isHeroCard[i])
            {
                _turnHandStrengths[i].isCalculated = false;
                continue;
            }
            
            isBoardCard[i] = true;
            counters.addCard(Card(i));
            counters.calculateAllHandStrengths();

            calcTurnHandStrengths(i, counters, isBoardCard);

            // Letting the river go
            for (int j = 0; j < 52; j++)
            {
                if (isBoardCard[j] || isHeroCard[j])
                    continue;

                isBoardCard[j] = true;
                counters.addCard(Card(j));
                counters.calculateAllHandStrengths();

                calcRiverHandStrengths(i, j, counters, isBoardCard);
                
                counters.removeLastCard();
                isBoardCard[j] = false;
            }

            sortRiverHandStrengths(i, isBoardCard);

            counters.removeLastCard();
            isBoardCard[i] = false;
        }
    }

    void AllHandStrengths::recalculate(const Card* board, int boardLen, const HoleCards& heroHoleCards)
    {
        class Worker
        {
            AllHandStrengths* _allHandStrengths;

        public:

            Worker(AllHandStrengths* allHandStrengths)
            {
                _allHandStrengths = allHandStrengths;
            }

            void operator() (const tbb::blocked_range<int>& br) const
            {
                _allHandStrengths->calcFlopHandStrenghts(br.begin(), br.end());
            }
        };

        assert (boardLen >= 3);

        _heroHoleCards = heroHoleCards;
        _boardLen = boardLen;

        for (int i=0; i<boardLen; i++)
            _board[i] = board[i];

        if (boardLen == 3)
        {
            if (USE_MT)
            {
                Worker worker(this);
                tbb::parallel_for(tbb::blocked_range<int>(0, 52), worker, tbb::auto_partitioner());
            }
            else
            {
                calcFlopHandStrenghts(0, 52);
            }
        }
        else // (boardLen > 3)
        {
            int turn = board[3].toInt();
            calcFlopHandStrenghts(turn, turn + 1);
        }
    }

    AllHandStrengths::AllHandStrengths()
    {
    }
}
