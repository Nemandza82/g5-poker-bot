#include <string>
#include <list>
#include "Common.h"
#include "Player.h"
#include "HoleCards.h"
#include "GameState.h"
#include "HandStrengthCounter.h"
#include "AllCounters.h"
#include "Pot.h"
#include "ParkMillerCarta.h"
#include "tbb/parallel_for.h"
#include "tbb/blocked_range.h"
#include "PreFlopEquity.h"
#include <algorithm>
#include <limits>


namespace G5Cpp
{
    namespace
    {
        const int MAX_PLAYERS = 6;

        const int SHOWDOWN_BIN_COUNT = 13260;
        const int SHOWDOWN_ITERATIONS = 1000;

        const int TCUTOFF_BIN_COUNT = 13260;
        const int TCUTOFF_ITERATIONS = 1000;

        const float NODE_CHANCE_CUTOFF = 0.0f; // 0.01f;

        struct DMStats
        {
            struct LeafLogNode
            {
                Street street;
                float chance;
                float ev;
                int nOpponents;
                int heroMoneyInThePot;
                float possibleWinnings;
                int numRaises;
            };

            int numTurnCutOffs;
            float turnCutOff_NumRiversChecked;
            float turnCutOff_NumPots;
            int turnCutOff_OppHist[MAX_PLAYERS];

            int numShowDowns;
            int showDown_NumValidRuns;
            int showDown_OppHist[MAX_PLAYERS];

            std::vector<LeafLogNode> leafLog;

            DMStats()
            {
                for (int i=0; i<MAX_PLAYERS; i++)
                {
                    showDown_OppHist[i] = 0;
                    turnCutOff_OppHist[i] = 0;
                }

                numTurnCutOffs = 0;
                turnCutOff_NumRiversChecked = 0;
                turnCutOff_NumPots = 0;

                numShowDowns = 0;
                showDown_NumValidRuns = 0;
            }

            void add(const DMStats& stats)
            {
                for (int i=0; i<MAX_PLAYERS; i++)
                {
                    showDown_OppHist[i] += stats.showDown_OppHist[i];
                    turnCutOff_OppHist[i] += stats.turnCutOff_OppHist[i];
                }

                numTurnCutOffs += stats.numTurnCutOffs;
                turnCutOff_NumRiversChecked += stats.turnCutOff_NumRiversChecked;
                turnCutOff_NumPots += stats.turnCutOff_NumPots;

                numShowDowns += stats.numShowDowns;
                showDown_NumValidRuns += stats.showDown_NumValidRuns;

                for (const auto& node : leafLog)
                {
                    leafLog.push_back(node);
                }
            }

            void saveToFile(const char* filename)
            {
                FILE* f = fopen(filename, "w");

                if (f)
                {
                    fprintf(f, "NumOpp, NumRaises, NodeChance, EV, PossWinn, MoneyInPot\n");

                    for (const auto& node : leafLog)
                    {
                        fprintf(f, "%d,\t%d,\t%.2f,\t%.2f,\t%.2f,\t%d\n", node.nOpponents, node.numRaises, node.chance, node.ev,
                            node.possibleWinnings, node.heroMoneyInThePot);
                    }

                    fclose(f);
                }
            }
        };

        float estimateEV(DMStats& stats, const GameState& prms);

        /// <summary>
        /// Racuna hero EV ako je hero na potezu.
        /// </summary>
        void estimateEV_HeroPlays(float& checkCallEV, float& betRaiseEV, DMStats& stats, const GameState& prms)
        {
            assert (prms.heroInd == prms.playerToActInd);

            checkCallEV = 0.0f;
            {
                GameState newPrms = prms.playerCheckCalls(0, 0, 1.0f);
                checkCallEV  = estimateEV(stats, newPrms);
                checkCallEV -= prms.getAmountToCall();
            }

            betRaiseEV = 0.0f;

            // Can hero raise
            if (prms.canNextPlayerRaise())
            {
                GameState newPrms = prms.playerBetRaises(0, 0, 1.0f);
                betRaiseEV = estimateEV(stats, newPrms);
                betRaiseEV -= (newPrms.hero().moneyInPot() - prms.hero().moneyInPot());
            }
        }

        /// <summary>
        /// Racuna EV ako je villain na potezu.
        /// </summary>
        float estimateEV_VillainPlays(DMStats& stats, const GameState& prms)
        {
            int amountToCall = prms.getAmountToCall();
            ActionDistribution ad = prms.getPlayerToActAD();

            float predFoldProb = ad.foldProb;
            float predCheckCallProb = ad.checkCallProb;
            float predBetRaiseProb = ad.betRaiseProb;

            if (prms.street > Street_PreFlop && false) // Predict actions only in post flop
            {
                if (amountToCall > 0)
                {
                    prms.playerToAct().predictAction_FoldCallRaise(predFoldProb, predCheckCallProb, predBetRaiseProb,
                        prms.board, ad.betRaiseProb, ad.checkCallProb, prms.gc);
                }
                else
                {
                    predFoldProb = 0.0f;
                    prms.playerToAct().predictAction_CheckBet(predCheckCallProb, predBetRaiseProb,
                        prms.board, ad.betRaiseProb, prms.gc);
                }
            }

            float checkCallProb = ad.checkCallProb;
            float betRaiseProb = ad.betRaiseProb;

            // Can villain raise
            if (!prms.canNextPlayerRaise())
            {
                checkCallProb += betRaiseProb;
                betRaiseProb = 0.0f;

                predCheckCallProb += predBetRaiseProb;
                predBetRaiseProb = 0.0f;
            }

            float foldEV = 0.0f;

            if ((amountToCall > 0) && (predFoldProb > 0.0f))
            {
                assert (prms.numActivePlayers() >= 2);

                GameState newPrms = prms.playerFolds(predFoldProb);
                foldEV = estimateEV(stats, newPrms);
            }

            float checkCallEV = 0.0f;
            {
                GameState newPrms = prms.playerCheckCalls(betRaiseProb, checkCallProb, predCheckCallProb);
                checkCallEV = estimateEV(stats, newPrms);
            }

            float betRaiseEV = 0.0f;

            if (prms.canNextPlayerRaise())
            {
                GameState newPrms = prms.playerBetRaises(betRaiseProb, checkCallProb, predBetRaiseProb);
                betRaiseEV = estimateEV(stats, newPrms);
            }

            return (predFoldProb * foldEV) + (predCheckCallProb * checkCallEV) + (predBetRaiseProb * betRaiseEV);
        }

        void estimateEV_NextStreet(float* EV, DMStats* stats, const GameState& prms, int begin, int end)
        {
            for (int i=begin; i<end; i++)
            {
                Card card(i);

                if (!prms.isBanned(card))
                {
                    GameState newPrms = prms.goToNextStreet(card);
                    EV[i] = estimateEV(stats[i], newPrms);
                }
            }
        }

        /// <summary>
        /// Goes to next street in EV calculation.
        /// </summary>
        float estimateEV_NextStreet_mt(DMStats& stats, const GameState& prms)
        {
            class Worker
            {
                float* EV;
                DMStats* stats;
                const GameState& prms;

            public:

                Worker(float* aEV, DMStats* aStats, const GameState& aPrms) : prms(aPrms)
                {
                    EV = aEV;
                    stats = aStats;
                }

                void operator() (const tbb::blocked_range<int>& br) const
                {
                    estimateEV_NextStreet(EV, stats, prms, br.begin(), br.end());
                }
            };

            float EV[52];
            DMStats statsArray[52];

            if (USE_MT)
            {
                Worker worker(EV, statsArray, prms);
                tbb::parallel_for(tbb::blocked_range<int>(0, 52), worker, tbb::auto_partitioner());
            }
            else
            {
                estimateEV_NextStreet(EV, statsArray, prms, 0, 52);
            }

            float totalEV = 0.0f;
            int cnt = 0;

            for (int i=0; i<52; i++)
            {
                if (!prms.isBanned(Card(i)))
                {
                    stats.add(statsArray[i]);
                    totalEV += EV[i];
                    cnt++;
                }
            }

            return totalEV / cnt;
        }

        /// <summary>
        /// Calculates EV when show down occurs
        /// </summary>
        float estimateEV_ShowDown(DMStats& stats, const GameState& prms)
        {
            int nOpponents = 0;
            const Player* opponents[MAX_PLAYERS];
            prms.getOpponents(opponents, nOpponents);

            stats.numShowDowns++;
            stats.showDown_OppHist[nOpponents]++;

            int turn = prms.board.card[3].toInt();
            int river = prms.board.card[4].toInt();

            float possibleWinnings = prms.getPossibleWinnings();
            const AllHandStrengths& handStrenghts = prms.gc.allHandStrengths();
            int heroHandStrength = handStrenghts.getRiverHandStrength(turn, river, prms.heroHoleCards.toInt());

            float EV = 0.0f;

            if (nOpponents == 0) // Everybody has folded except us -> go out
            {
                EV = possibleWinnings;
            }
            else if (nOpponents == 1)
            {
                int oppRangeLength = opponents[0]->range().length();
                const int* oppRangeHCInd = opponents[0]->range().hcIndex();
                const float* oppRangeLikelihood = opponents[0]->range().likelihood();

                float totalWinnings = 0.0f;
                int cnt = 0;

                for (int i = 0; i < oppRangeLength; i++)
                {
                    float eq = oppRangeLikelihood[i];

                    if (eq == 0.0f)
                        continue;

                    //HoleCards oppHC(range.ind[i]);
                    int oppHS = handStrenghts.getRiverHandStrength(turn, river, oppRangeHCInd[i]);

                    if (heroHandStrength > oppHS)
                    {
                        totalWinnings += eq * possibleWinnings;
                    }
                    else if (heroHandStrength == oppHS)
                    {
                        totalWinnings += eq * possibleWinnings / 2.0f;
                    }

                    cnt++;
                }

                stats.showDown_NumValidRuns += cnt;
                EV = totalWinnings;
            }
            else
            {
                int opponentHandIndexes[MAX_PLAYERS * SHOWDOWN_BIN_COUNT];

                for (int i=0; i<nOpponents; i++)
                {
                    opponents[i]->range_FillHandIndices(&opponentHandIndexes[i*SHOWDOWN_BIN_COUNT], SHOWDOWN_BIN_COUNT);
                }

                HoleCards opponentHoleCards[MAX_PLAYERS];
                Pot pot(prms._players);
                ParkMillerCarta rng(1);

                float totalWinnings = 0.0f;
                int nIter = 0;
                bool isOppCard[52];

                for (int j=0; j<52; j++)
                    isOppCard[j] = false;

                for (int i=0; i<SHOWDOWN_ITERATIONS; i++)
                {
                    // Choose opponent hole cards randomly
                    for (int j=0; j<nOpponents; j++)
                    {
                        int index = rng.next() % SHOWDOWN_BIN_COUNT;
                        opponentHoleCards[j] = HoleCards(opponentHandIndexes[j*SHOWDOWN_BIN_COUNT + index]);
                    }

                    bool valid = true;

                    // Ban choosen hole cards and check if combinationis is valid
                    for (int j=0; j<nOpponents; j++)
                    {
                        int ind0 = opponentHoleCards[j].Card0.toInt();
                        int ind1 = opponentHoleCards[j].Card1.toInt();

                        if (isOppCard[ind0] || isOppCard[ind1])
                            valid = false;

                        isOppCard[ind0] = true;
                        isOppCard[ind1] = true;
                    }

                    // If combination is valid calculate EV
                    if (valid)
                    {
                        for (int j=0; j<nOpponents; j++)
                        {
                            int opponentHandStrength = handStrenghts.getRiverHandStrength(turn, river, opponentHoleCards[j].toInt());
                            pot.addHandStrength(opponentHandStrength, opponents[j]->moneyInPot());
                        }

                        pot.addHandStrength(heroHandStrength, prms.hero().moneyInPot());
                        totalWinnings += pot.calculateWinnings(heroHandStrength, prms.hero().moneyInPot());
                        nIter++;
                    }

                    // Un-ban chosen hole cards
                    for (int j=0; j<nOpponents; j++)
                    {
                        int ind0 = opponentHoleCards[j].Card0.toInt();
                        int ind1 = opponentHoleCards[j].Card1.toInt();

                        isOppCard[ind0] = false;
                        isOppCard[ind1] = false;
                    }

                    // Reset the pot
                    pot.reset();
                }

                assert (nIter > 0);
                nIter = std::max(nIter, 1);

                stats.showDown_NumValidRuns += nIter;
                EV = totalWinnings / nIter;
            }

            return EV;
        }

        inline void updateAhead(float& ahead, int heroStrength, int oppStrength)
        {
            if (heroStrength > oppStrength)
            {
                ahead += 1.0f;
            }
            else if (heroStrength == oppStrength)
            {
                ahead += 0.5f;
            }
        }

        float estimateEV_TurnCutoff(DMStats& stats, const GameState& prms)
        {
            int nOpponents = 0;
            const Player* opponents[MAX_PLAYERS];
            prms.getOpponents(opponents, nOpponents);

            stats.numTurnCutOffs++;
            stats.turnCutOff_OppHist[nOpponents]++;

            int turn = prms.board.card[3].toInt();

            const AllHandStrengths& handStrenghts = prms.gc.allHandStrengths();

            int heroCurrentHandStrength = handStrenghts.getTurnHandStrength(turn, prms.heroHoleCards.toInt());
            const int* heroSortedRivers = handStrenghts.getSortedRivers(turn, prms.heroHoleCards.toInt());
            const int* heroRiverStrengths = handStrenghts.getRiverHandStrengths(turn, prms.heroHoleCards.toInt());

            bool isHeroCard[52];
            bool isOppCard[52];
            bool isBoardCard[52];

            for (int i=0; i<52; i++)
            {
                isHeroCard[i] = false;
                isOppCard[i] = false;
                isBoardCard[i] = false;
            }

            isHeroCard[prms.heroHoleCards.Card0.toInt()] = true;
            isHeroCard[prms.heroHoleCards.Card1.toInt()] = true;

            isBoardCard[prms.board.card[0].toInt()] = true;
            isBoardCard[prms.board.card[1].toInt()] = true;
            isBoardCard[prms.board.card[2].toInt()] = true;
            isBoardCard[prms.board.card[3].toInt()] = true;

            float EV = 0.0f;

            if (nOpponents == 0) // Everybody has folded except us -> go out
            {
                EV = prms.getPossibleWinnings();
            }
            else if (nOpponents == 1)
            {
                int oppRangeLength = opponents[0]->range().length();
                const int* oppRangeHCInd = opponents[0]->range().hcIndex();
                const float* oppRangeLikelihood = opponents[0]->range().likelihood();

                float possibleWinnings = prms.getPossibleWinnings();
                float totalWinnings = 0.0f;

                int nRiversChecked = 0;
                int nIter = 0;

                for (int i = 0; i < oppRangeLength; i++)
                {
                    if (oppRangeLikelihood[i] == 0.0f)
                        continue;

                    HoleCards oppHoleCards = HoleCards(oppRangeHCInd[i]);
                    isOppCard[oppHoleCards.Card0.toInt()] = true;
                    isOppCard[oppHoleCards.Card1.toInt()] = true;

                    int oppCurrentHandStrength = handStrenghts.getTurnHandStrength(turn, oppRangeHCInd[i]);
                    const int* oppSortedRivers = handStrenghts.getSortedRivers(turn, oppRangeHCInd[i]);
                    const int* oppRiverStrengths = handStrenghts.getRiverHandStrengths(turn, oppRangeHCInd[i]);

                    float ahead = 0;

                    if (heroCurrentHandStrength < oppCurrentHandStrength) // Hero is behind
                    {
                        for (int k=0, river=heroSortedRivers[0];; river=heroSortedRivers[++k])
                        {
                            int heroStrength = heroRiverStrengths[river];

                            if (heroStrength == -1 || heroStrength < oppCurrentHandStrength)
                                break;

                            if (isOppCard[river])
                                continue;

                            assert (!isBoardCard[river]);

                            nRiversChecked++;
                            updateAhead(ahead, heroStrength, oppRiverStrengths[river]);
                        }
                    }
                    else if (heroCurrentHandStrength > oppCurrentHandStrength) // Opp is behind
                    {
                        ahead = 44;

                        for (int k=0, river=oppSortedRivers[0];; river=oppSortedRivers[++k])
                        {
                            int oppStrength = oppRiverStrengths[river];

                            if (oppStrength == -1 || oppStrength < heroCurrentHandStrength)
                                break;

                            if (isHeroCard[river])
                                continue;

                            assert (!isBoardCard[river]);

                            ahead--;
                            nRiversChecked++;
                            updateAhead(ahead, heroRiverStrengths[river], oppStrength);
                        }
                    }
                    else // Its tie, check all cards
                    {
                        for (int river=0; river<52; river++)
                        {
                            if (isHeroCard[river] || isOppCard[river] || isBoardCard[river])
                                continue;

                            nRiversChecked++;
                            updateAhead(ahead, heroRiverStrengths[river], oppRiverStrengths[river]);
                        }
                    }

                    isOppCard[oppHoleCards.Card0.toInt()] = false;
                    isOppCard[oppHoleCards.Card1.toInt()] = false;

                    totalWinnings += oppRangeLikelihood[i] * possibleWinnings * (ahead / 44);
                    nIter++;
                }

                stats.turnCutOff_NumPots += 1;
                stats.turnCutOff_NumRiversChecked += nRiversChecked / (float)nIter;
                EV = totalWinnings;
            }
            else // nOpponents >= 2
            {
                int opponentHandIndexes[MAX_PLAYERS * TCUTOFF_BIN_COUNT];

                for (int i=0; i<nOpponents; i++)
                {
                    opponents[i]->range_FillHandIndices(&opponentHandIndexes[i*TCUTOFF_BIN_COUNT], TCUTOFF_BIN_COUNT);
                }

                int opponentHoleCardsInd[MAX_PLAYERS];
                HoleCards opponentHoleCards[MAX_PLAYERS];
                Pot pot(prms._players);
                ParkMillerCarta rng(1);

                float totalWinnings = 0.0f;
                int nRiversChecked = 0;
                int nIter = 0;

                for (int i=0; i<TCUTOFF_ITERATIONS; i++)
                {
                    // Choose opponent hole cards randomly
                    for (int j=0; j<nOpponents; j++)
                    {
                        int ind = rng.next() % TCUTOFF_BIN_COUNT;
                        opponentHoleCardsInd[j] = opponentHandIndexes[j*TCUTOFF_BIN_COUNT + ind];
                        opponentHoleCards[j] = HoleCards(opponentHoleCardsInd[j]);
                    }

                    bool iterationValid = true;

                    // Ban choosen hole cards and check if combinationis is valid
                    for (int j=0; j<nOpponents; j++)
                    {
                        int ind0 = opponentHoleCards[j].Card0.toInt();
                        int ind1 = opponentHoleCards[j].Card1.toInt();

                        if (isOppCard[ind0] || isOppCard[ind1])
                            iterationValid = false;

                        isOppCard[ind0] = true;
                        isOppCard[ind1] = true;
                    }

                    // If combination is valid calculate EV... Choose some rivers to check...
                    if (iterationValid)
                    {
                        int oppCurrentHandStrength[MAX_PLAYERS];
                        const int* oppSortedRivers[MAX_PLAYERS];
                        const int* oppRiverStrengths[MAX_PLAYERS];

                        for (int j=0; j<nOpponents; j++)
                        {
                            oppCurrentHandStrength[j] = handStrenghts.getTurnHandStrength(turn, opponentHoleCardsInd[j]);
                            oppSortedRivers[j] = handStrenghts.getSortedRivers(turn, opponentHoleCardsInd[j]);
                            oppRiverStrengths[j] = handStrenghts.getRiverHandStrengths(turn, opponentHoleCardsInd[j]);
                        }

                        // For all pots and side pots calculate separatelly...
                        for (int ip=0; ip<pot.numPots(); ip++)
                        {
                            if (prms.hero().moneyInPot() < pot.getHeight(ip))
                                break;

                            int maxOppCurrHandStrength = 0;

                            for (int j=0; j<nOpponents; j++)
                            {
                                if (opponents[j]->moneyInPot() >= pot.getHeight(ip))
                                    maxOppCurrHandStrength = std::max(maxOppCurrHandStrength, oppCurrentHandStrength[j]);
                            }

                            // If there is no-one but us fighting for this pot continue
                            if (maxOppCurrHandStrength == 0)
                            {
                                totalWinnings += pot.getMoney(ip);
                                continue;
                            }

                            float ahead = 0;
                            bool riverToCheck[52];

                            for (int k=0; k<52; k++)
                            {
                                riverToCheck[k] = false;
                            }

                            if (heroCurrentHandStrength < maxOppCurrHandStrength) // Hero is behind, check all rivers that make us possible ahead
                            {
                                for (int k=0, river=heroSortedRivers[0];; river=heroSortedRivers[++k])
                                {
                                    int heroStrength = heroRiverStrengths[river];

                                    if (heroStrength == -1 || heroStrength < maxOppCurrHandStrength)
                                        break;

                                    if (isOppCard[river])
                                        continue;

                                    assert (!isBoardCard[river]);
                                    riverToCheck[river] = true;
                                }
                            }
                            else if (heroCurrentHandStrength > maxOppCurrHandStrength) // Hero is ahead, check all cards that make oppontent ahead
                            {
                                ahead = (float) (46 - nOpponents*2);

                                for (int j=0; j<nOpponents; j++)
                                {
                                    if (opponents[j]->moneyInPot() < pot.getHeight(ip))
                                        continue;

                                    for (int k=0, river=oppSortedRivers[j][0];; river=oppSortedRivers[j][++k])
                                    {
                                        int oppStrength = oppRiverStrengths[j][river];

                                        if (oppStrength == -1 || oppStrength < heroCurrentHandStrength)
                                            break;

                                        if (isHeroCard[river] || isOppCard[river] || riverToCheck[river])
                                            continue;

                                        assert (!isBoardCard[river]);

                                        ahead -= 1;
                                        riverToCheck[river] = true;
                                    }
                                }
                            }
                            else // heroCurrentHandStrength == maxOppCurrHandStrength, check all rivers
                            {
                                for (int river=0; river<52; river++)
                                {
                                    if (isHeroCard[river] || isOppCard[river] || isBoardCard[river])
                                        continue;

                                    riverToCheck[river] = true;
                                }
                            }

                            for (int river=0; river<52; river++)
                            {
                                if (!riverToCheck[river])
                                    continue;

                                int maxOppStrength = 0;
                                int tiedOpponents = 0;

                                for (int j=0; j<nOpponents; j++)
                                {
                                    if (opponents[j]->moneyInPot() < pot.getHeight(ip))
                                        continue;

                                    int oppStrength = oppRiverStrengths[j][river];

                                    if (oppStrength > maxOppStrength)
                                    {
                                        maxOppStrength = oppStrength;
                                        tiedOpponents = 1;
                                    }
                                    else if (oppStrength == maxOppStrength)
                                    {
                                        tiedOpponents++;
                                    }
                                }

                                if (heroRiverStrengths[river] > maxOppStrength)
                                {
                                    ahead += 1.0f;
                                }
                                else if (heroRiverStrengths[river] == maxOppStrength)
                                {
                                    ahead += 1.0f / (tiedOpponents + 1);
                                }

                                nRiversChecked++;
                            }

                            totalWinnings += pot.getMoney(ip) * ahead / (46 - nOpponents*2);
                        }

                        nIter++;
                    }

                    // Un-ban chosen hole cards
                    for (int j=0; j<nOpponents; j++)
                    {
                        int ind0 = opponentHoleCards[j].Card0.toInt();
                        int ind1 = opponentHoleCards[j].Card1.toInt();

                        isOppCard[ind0] = false;
                        isOppCard[ind1] = false;
                    }
                }

                assert (nIter > 0);
                nIter = std::max(nIter, 1);

                stats.turnCutOff_NumRiversChecked += nRiversChecked / (float)nIter;
                stats.turnCutOff_NumPots += pot.numPots();

                EV = totalWinnings / nIter;
            }

            return EV;
        }

        float estimateEV_PreFlopCutoff(DMStats& stats, const GameState& prms)
        {
            int nOpponents = 0;
            const Player* opponents[MAX_PLAYERS];
            prms.getOpponents(opponents, nOpponents);

            float EV = 0.0f;

            if (nOpponents == 0)
            {
                EV = prms.getPossibleWinnings();
            }
            else if (nOpponents == 1)
            {
                float equity = PreFlopEquity::calculate(prms.heroHoleCards, opponents[0]->range());
                EV = equity * prms.getPossibleWinnings();
            }
            else // (nOpponents >= 2)
            {
                float minEquity = std::numeric_limits<float>::max();

                for (int i=0; i<nOpponents; i++)
                {
                    float equity = PreFlopEquity::calculate(prms.heroHoleCards, opponents[i]->range());
                    minEquity = std::min(minEquity, equity);
                }

                float mod;

                if (nOpponents == 2)
                    mod = 0.8f;
                else if (nOpponents == 3)
                    mod = 0.7f;
                else if (nOpponents == 4)
                    mod = 0.6f;
                else if (nOpponents == 5)
                    mod = 0.5f;

                EV = (mod * minEquity) * prms.getPossibleWinnings();
            }

            // If there are opponents and we have some money left...
            if (nOpponents > 0 && prms.hero().stack() > 0)
            {
                if (prms.isPlayerInPosition(prms.heroInd))
                {
                    // In position
                    EV *= 1.0f;
                }
                else if (prms.isHeroFirstToAct_postFlop())
                {
                    // First to act
                    EV *= 0.85f;
                }
                else
                {
                    // Some other position
                    EV *= 0.90f;
                }
            }

            DMStats::LeafLogNode lln;

            lln.chance = prms.nodeChance;
            lln.ev = EV;
            lln.nOpponents = nOpponents;
            lln.street = Street_PreFlop;
            lln.heroMoneyInThePot = prms.hero().moneyInPot();
            lln.possibleWinnings = prms.getPossibleWinnings();
            lln.numRaises = prms.numBets;

            stats.leafLog.push_back(lln);
            return EV;
        }

        /// <summary>
        /// Calculates post flop EV
        /// </summary>
        float estimateEV(DMStats& stats, const GameState& prms)
        {
            if (prms.nodeChance <= NODE_CHANCE_CUTOFF)
            {
                return 0.0f;
            }

            if (prms.playerToActInd == -1) // Go to next street
            {
                if (prms.street == Street_PreFlop)
                {
                    return estimateEV_PreFlopCutoff(stats, prms);
                }
                else if (prms.street == Street_River) // ShowDown
                {
                    return estimateEV_ShowDown(stats, prms);
                }
                else // Go to next street
                {
                    // If the current street is turn and estimation started on flop, cut it.
                    // But, if we have just two players, don't stop on turn. Go all the way down.
                    if (prms.street == Street_Turn && prms.stertedOnFlop && prms.startNumActive > 2)
                    {
                        return estimateEV_TurnCutoff(stats, prms);
                    }
                    else
                    {
                        return estimateEV_NextStreet_mt(stats, prms);
                    }
                }
            }
            else if (prms.playerToActInd == prms.heroInd) // Mi igramo
            {
                float foldEV = 0.0f;
                float checkCallEV = 0.0f;
                float betRaiseEV = 0.0f;
                estimateEV_HeroPlays(checkCallEV, betRaiseEV, stats, prms);

                float EV = std::max(checkCallEV, std::max(foldEV, betRaiseEV));
                return EV;
            }
            else // (playerToAct != hero)
            {
                return estimateEV_VillainPlays(stats, prms);
            }
        }
    } // namespace

    extern "C" G5_EXPORT void __stdcall EstimateEV(float& checkCallEV, float& betRaiseEV, int buttonInd, int heroIndex, const HoleCards& heroHoleCards,
        const PlayerDTO* players, int nPlayers, const Card* cardsInBoard, Street street, int numBets, int numCallers, int bigBlindSize, const void* gc)
    {
        // Get the GameContext
        const GameContext* gcPtr = static_cast<const GameContext*>(gc);

        Board board(cardsInBoard, street);

        // Assert if game context is built for the right flop and hero HoleCards
        gcPtr->assertBoard(board);
        TableType tt = TableType(players->_model.totalPlayers);

        DMStats stats;
        {
            GameState prms(tt, buttonInd, heroIndex, heroHoleCards, players, nPlayers, board, street, numBets, numCallers, bigBlindSize, *gcPtr);

            switch (street)
            {
            case Street_River:
                prms.BETS_CUTOFF_POST_FLOP = 3;
                break;
            case Street_Turn:
                prms.BETS_CUTOFF_POST_FLOP = (prms.startNumActive >= 4) ? 2 : 3;
                break;
            case Street_Flop:
                // If there are 2 active players we will calculate to showdown, thats why go only 2 raises deep...
                prms.BETS_CUTOFF_POST_FLOP = (prms.startNumActive == 2 || prms.startNumActive >= 4) ? 2 : 3;
                break;
            }

            estimateEV_HeroPlays(checkCallEV, betRaiseEV, stats, prms);
        }

        // Save stats of the calculation
        //stats.SaveToFile("c:\\temp\\dmstats.txt");
    }
}
