#include "GameContext.h"
#include "PreFlopEquity.h"
#include "AllCounters.h"
#include "tbb/parallel_reduce.h"
#include "tbb/blocked_range.h"
#include <algorithm>
#include <vector>
#include <assert.h>


namespace G5Cpp
{
    namespace
    {
        struct EquitySortPair
        {
            int ind;
            float equity;

            EquitySortPair()
            {
                ind = 0;
                equity = 0;
            }

            EquitySortPair(int index, float e)
            {
                ind = index;
                equity = e;
            }

            // Descending compare
            static int Compare(const void* a, const void* b)
            {
                EquitySortPair* A = (EquitySortPair*)a;
                EquitySortPair* B = (EquitySortPair*)b;

                if (B->equity < A->equity)
                {
                    return -1;
                }
                else if (B->equity > A->equity)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }

            static bool Less(const EquitySortPair& A, const EquitySortPair& B)
            {
                return (B.equity < A.equity);
            }
        };

        void SortEquities(EquitySortPair* pairs, int nPairs)
        {
            if (false)
            {
                // Stable sort implementation
                std::vector<EquitySortPair> myvector;
                myvector.assign(pairs, pairs+nPairs);
                std::stable_sort(myvector.begin(), myvector.end(), EquitySortPair::Less);
            }
            else
            {
                // Unstable but faster
                qsort(pairs, nPairs, sizeof(EquitySortPair), EquitySortPair::Compare);
            }
        }

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

            static bool Less(const StrengthSortPair& A, const StrengthSortPair& B)
            {
                return (B.strength < A.strength);
            }
        };

        void SortStrengthPairs(StrengthSortPair* pairs, int nPairs, const AllCounters& counters, const bool* isBoardCard)
        {
            int k = 0;

            for (int i = 0; i < 51; i++) // Prva karta u ruci
            {
                for (int j = i + 1; j < 52; j++) // Druga karta u ruci
                {
                    if (!isBoardCard[i] && !isBoardCard[j])
                    {
                        int ind = i * 52 + j;
                        int handStrength = counters.getHandStrength(ind);

                        assert (k < nPairs);
                        pairs[k++] = StrengthSortPair(ind, handStrength);
                    }
                }
            }

            assert (k == nPairs);

            if (false)
            {
                // Stable sort implementation
                std::vector<StrengthSortPair> myvector;
                myvector.assign(pairs, pairs+nPairs);
                std::stable_sort(myvector.begin(), myvector.end(), StrengthSortPair::Less);
            }
            else
            {
                // Unstable but faster
                qsort(pairs, nPairs, sizeof(StrengthSortPair), StrengthSortPair::Compare);
            }
        }

        void SortAtRiver(SortedHoleCards& sortedResult, float* turnEquities, int* turnCounts, const AllCounters& counters, const bool* isBoardCard)
        {
            StrengthSortPair sortedPairs[N_HOLECARDS_RIVER];
            SortStrengthPairs(sortedPairs, N_HOLECARDS_RIVER, counters, isBoardCard);

            EquitySortPair tmpResult[N_HOLECARDS_RIVER];

            for (int i=0; i<N_HOLECARDS_RIVER; i++)
            {
                tmpResult[i].ind = sortedPairs[i].ind;
            }

            int tieBehindOcc[52];
            int tieOcc[52];

            for (int i = 0; i < 52; i++)
            {
                tieBehindOcc[i] = 0;
            }

            for (int i = 0; i < N_HOLECARDS_RIVER; i++)
            {
                int c1 = sortedPairs[i].ind / 52;
                int c2 = sortedPairs[i].ind % 52;
                tieBehindOcc[c1]++;
                tieBehindOcc[c2]++;
            }

            for (int i = 0, j = 1; j < N_HOLECARDS_RIVER; )
            {
                while (j < N_HOLECARDS_RIVER && sortedPairs[i].strength == sortedPairs[j].strength)
                {
                    j++;
                }

                for (int l = 0; l < 52; l++)
                {
                    tieOcc[l] = 0;
                }

                for (int m = i; m < j; m++)
                {
                    int c1 = sortedPairs[m].ind / 52;
                    int c2 = sortedPairs[m].ind % 52;
                    tieOcc[c1]++;
                    tieOcc[c2]++;
                }

                for (int m = i; m < j; m++)
                {
                    int ind = sortedPairs[m].ind;
                    int c1 = ind / 52;
                    int c2 = ind % 52;

                    {
                        int ahead = i - (46 - tieBehindOcc[c1]) - (46 - tieBehindOcc[c2]);
                        int tie = (j - i) - (tieOcc[c1] - 1) - (tieOcc[c2] - 1) - 1;
                        int behind = (N_HOLECARDS_RIVER - j) - (tieBehindOcc[c1] - tieOcc[c1]) - (tieBehindOcc[c2] - tieOcc[c2]);

                        assert (ahead + tie + behind == 990);

                        float equity = (behind + tie / 2.0f) / (ahead + tie + behind);

                        // EV^2 improvement from POLARIS.
                        equity *= equity;

                        tmpResult[m].equity = equity;
                        turnEquities[ind] += equity;
                        turnCounts[ind]++;
                    }
                }

                for (int m = i; m < j; m++)
                {
                    int c1 = sortedPairs[m].ind / 52;
                    int c2 = sortedPairs[m].ind % 52;
                    tieBehindOcc[c1]--;
                    tieBehindOcc[c2]--;
                }

                i = j;
                j++;
            }

            // Re-sort
            {
                SortEquities(tmpResult, N_HOLECARDS_RIVER);

                for (int i=0; i<N_HOLECARDS_RIVER; i++)
                {
                    sortedResult.equity[i] = tmpResult[i].equity;
                    sortedResult.ind[i] = tmpResult[i].ind;
                }
            }
        }

        void SortAtTurn(SortedHoleCards& sortedResult, float* flopEquities, int* flopCounts, const float* turnEquities, const int* turnCounts, const bool* isBoardCard)
        {
            EquitySortPair pairs[N_HOLECARDS_TURN];
            int k = 0;

            for (int i = 0; i < 51; i++) // Prva karta u ruci
            {
                for (int j = i + 1; j < 52; j++) // Druga karta u ruci
                {
                    if (!isBoardCard[i] && !isBoardCard[j])
                    {
                        int ind = i * 52 + j;

                        float equity = turnEquities[ind] / turnCounts[ind];
                        pairs[k++] = EquitySortPair(ind, equity);

                        flopEquities[ind] += turnEquities[ind];
                        flopCounts[ind] += turnCounts[ind];
                    }
                }
            }

            assert(k == N_HOLECARDS_TURN);

            SortEquities(pairs, N_HOLECARDS_TURN);

            for (int i = 0; i < N_HOLECARDS_TURN; i++)
            {
                sortedResult.equity[i] = pairs[i].equity;
                sortedResult.ind[i] = pairs[i].ind;
            }
        }

        void SortAtFlop(SortedHoleCards& sortedResult, float* flopEquities, const int* flopCount, const Card flopCards[3])
        {
            bool isBoardCard[52];

            for (int i = 0; i < 52; i++)
            {
                isBoardCard[i] = false;
            }

            isBoardCard[flopCards[0].toInt()] = true;
            isBoardCard[flopCards[1].toInt()] = true;
            isBoardCard[flopCards[2].toInt()] = true;

            EquitySortPair pairs[N_HOLECARDS_FLOP];
            int k = 0;

            for (int i = 0; i < 51; i++) // Prva karta u ruci
            {
                for (int j = i + 1; j < 52; j++) // Druga karta u ruci
                {
                    if (!isBoardCard[i] && !isBoardCard[j])
                    {
                        int ind = i * 52 + j;

                        flopEquities[ind] /= flopCount[ind];
                        pairs[k++] = EquitySortPair(ind, flopEquities[ind]);
                    }
                }
            }

            assert (k == N_HOLECARDS_FLOP);

            SortEquities(pairs, N_HOLECARDS_FLOP);

            for (int i = 0; i < N_HOLECARDS_FLOP; i++)
            {
                sortedResult.equity[i] = pairs[i].equity;
                sortedResult.ind[i] = pairs[i].ind;
            }
        }
    } // namespace

    void GameContext::sortHoleCards(float* flopEquities, int* flopCounts, int tStart, int tEnd)
    {
        Card board[5];
        board[0] = _flopCards[0];
        board[1] = _flopCards[1];
        board[2] = _flopCards[2];

        AllCounters counters;
        counters.addCard(board[0]);
        counters.addCard(board[1]);
        counters.addCard(board[2]);

        bool isBoardCard[52];

        for (int i = 0; i < 52; i++)
        {
            isBoardCard[i] = false;
        }

        isBoardCard[board[0].toInt()] = true;
        isBoardCard[board[1].toInt()] = true;
        isBoardCard[board[2].toInt()] = true;

        for (int i = tStart; i < tEnd; i++) // Moguci turnovi
        {
            if (isBoardCard[i])
                continue;

            isBoardCard[i] = true;
            board[3] = Card(i);

            float turnEquities[N_HOLECARDS_DOUBLE];
            int turnCounts[N_HOLECARDS_DOUBLE];

            for (int k = 0; k < N_HOLECARDS_DOUBLE; k++)
            {
                turnEquities[k] = 0;
                turnCounts[k] = 0;
            }

            counters.addCard(Card(i));

            for (int j = 0; j < 52; j++) // Moguci riveri
            {
                if (isBoardCard[j])
                    continue;

                int turnRiverIndex = i * 52 + j;

                isBoardCard[j] = true;
                board[4] = Card(j);

                counters.addCard(Card(j));
                counters.calculateAllHandStrengths();

                {
                    if (_sortedOnRiver[turnRiverIndex].length == 0)
                    {
                        _sortedOnRiver[turnRiverIndex].setLength(N_HOLECARDS_RIVER);
                        _sortedOnRiver[turnRiverIndex].clearEquities();
                    }

                    SortAtRiver(_sortedOnRiver[turnRiverIndex], turnEquities, turnCounts, counters, isBoardCard);
                }

                counters.removeLastCard();
                isBoardCard[j] = false;
            }

            if (_sortedOnTurn[i].length == 0)
            {
                _sortedOnTurn[i].setLength(N_HOLECARDS_TURN);
                _sortedOnTurn[i].clearEquities();
            }

            SortAtTurn(_sortedOnTurn[i], flopEquities, flopCounts, turnEquities, turnCounts, isBoardCard);

            counters.removeLastCard();
            isBoardCard[i] = false;
        }
    }

    void GameContext::sortHoleCards(const Card* flopCards)
    {
        class Worker
        {
            GameContext& _gameContext;

        public:

            float flopEquities[N_HOLECARDS_DOUBLE];
            int flopCounts[N_HOLECARDS_DOUBLE];

            Worker(GameContext& gameContext) : _gameContext(gameContext)
            {
                for (int i = 0; i < N_HOLECARDS_DOUBLE; i++)
                {
                    flopCounts[i] = 0;
                    flopEquities[i] = 0;
                }
            }

            Worker(Worker& worker, tbb::split) : _gameContext(worker._gameContext)
            {
                for (int i = 0; i < N_HOLECARDS_DOUBLE; i++)
                {
                    flopCounts[i] = 0;
                    flopEquities[i] = 0;
                }
            }

            void operator() (const tbb::blocked_range<int>& br)
            {
                _gameContext.sortHoleCards(flopEquities, flopCounts, br.begin(), br.end());
            }

            void join(Worker& that)
            {
                for (int i = 0; i < N_HOLECARDS_DOUBLE; i++)
                {
                    flopCounts[i] += that.flopCounts[i];
                    flopEquities[i] += that.flopEquities[i];
                }
            }
        };

        _flopCards[0] = flopCards[0];
        _flopCards[1] = flopCards[1];
        _flopCards[2] = flopCards[2];

        Worker worker(*this);

        if (USE_MT)
        {
            tbb::parallel_reduce(tbb::blocked_range<int>(0, 52), worker);
        }
        else
        {
            worker(tbb::blocked_range<int>(0, 52));
        }

        if (_sortedOnFlop.length == 0)
        {
            _sortedOnFlop.setLength(N_HOLECARDS_FLOP);
            _sortedOnFlop.clearEquities();
        }

        SortAtFlop(_sortedOnFlop, worker.flopEquities, worker.flopCounts, _flopCards);
    }

    void GameContext::newFlop(const Card* flop, const HoleCards* hc)
    {
        _flopCards[0] = flop[0];
        _flopCards[1] = flop[1];
        _flopCards[2] = flop[2];

        sortHoleCards(flop);

        if (hc)
        {
            _heroHoleCards = *hc;
            _allHandStrengths.recalculate(flop, 3, *hc);
        }
    }

    const SortedHoleCards& GameContext::sortedHoleCards(const Board& board) const
    {
        assertBoard(board);

        if (board.size < 3)
        {
            return _sortedPreFlop;
        }
        else if (board.size == 3)
        {
            return _sortedOnFlop;
        }
        else if (board.size == 4)
        {
            return _sortedOnTurn[board.card[3].toInt()];
        }
        else //if (street == Street_River)
        {
            int i = board.card[3].toInt();
            int j = board.card[4].toInt();

            return _sortedOnRiver[i * 52 + j];
        }
    }

    void GameContext::assertBoard(const Board& board) const
    {
        if (board.size >= 3)
        {
            assert(_flopCards[0].rank() == board.card[0].rank());
            assert(_flopCards[1].rank() == board.card[1].rank());
            assert(_flopCards[2].rank() == board.card[2].rank());

            assert(_flopCards[0].suite() == board.card[0].suite());
            assert(_flopCards[1].suite() == board.card[1].suite());
            assert(_flopCards[2].suite() == board.card[2].suite());
        }
    }

    GameContext::GameContext(std::string binPath)
    {
        // Load PreFlopEquities.txt file, where pre flop equities are stored.
        PreFlopEquity::load(binPath);

        _sortedPreFlop.setLength(N_HOLECARDS);
        PreFlopEquity::getSortedHoleCards(_sortedPreFlop);
    }
}
