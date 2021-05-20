#pragma once
#include "Common.h"
#include "Player.h"
#include "HoleCards.h"
#include "GameContext.h"
#include <vector>


namespace G5Cpp
{
    class GameState
    {
        void goToNextPlayer();
        void goToFirstPlayer();
        Player& playerToAct();
        void resetActedPlayersAfterRaise();

        int maxMoneyInThePot() const;
        bool areBetsLeft() const;
        int potSize() const;
        int getRaiseAmmount() const;
        int numActiveNonAllInPlayers() const;

    public:
        const GameContext& gc;
        std::vector<Player> _players;
        Board board;
        TableType _tableType;

        int playerToActInd;
        int heroInd;
        int buttonInd;
        HoleCards heroHoleCards;
        Street street;
        int numBets;
        int numCallers;

        // Number of bets when the evaluation started
        int startNumBets;
        int startNumActive;
        int bigBlindSize;

        // True if evaluation started on or before flop
        bool stertedOnFlop;
        float nodeChance;

        int BETS_CUTOFF_POST_FLOP = 2;

        GameState(TableType tableType, int buttonInd, int heroIndex, const HoleCards& heroHoleCards, const PlayerDTO* players, int nPlayers, const Board& board, Street street,
            int numBets, int numCallers, int bigBlindSize, const GameContext& aGc);

        bool canNextPlayerRaise() const;
        int getAmountToCall() const;
        bool isBanned(const Card& card) const;

        const Player& hero() const;
        const Player& playerToAct() const;
        int numActivePlayers() const;
        bool isPlayerInPosition(int playerIndex) const;
        bool isHeroFirstToAct_postFlop() const;
        ActionDistribution getPlayerToActAD() const;

        void getOpponents(const Player** opponents, int& nOpponents) const;
        float getPossibleWinnings() const;

        GameState goToNextStreet(const Card& card) const;
        GameState playerCheckCalls(float betRaiseProb, float checkCallProb, float nodeProbability) const;
        GameState playerBetRaises(float betRaiseProb, float checkCallProb, float nodeProbability) const;
        GameState playerFolds(float nodeProbability) const;
    };
}
