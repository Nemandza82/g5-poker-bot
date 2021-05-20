#pragma once
#include "Common.h"
#include <string>
#include "Range.h"
#include "PlayerModel.h"
#include "GameContext.h"
#include <memory>


namespace G5Cpp
{
    struct PlayerDTO
    {
        int _id;
        Status _statusInHand;
        ActionType _lastAction;
        ActionType _prevStreetAction;
        Position _preFlopPosition;

        int _stack;
        int _moneyInPot;

        PlayerModel _model;
        Range _range;
    };

    class Player
    {
        int _id;
        Status _statusInHand;
        ActionType _lastAction;
        ActionType _prevStreetAction;
        Position _preFlopPosition;

        int _stack;
        int _moneyInPot;

        std::shared_ptr<PlayerModel> _model;
        std::shared_ptr<Range> _range;

    public:

        Player()
        {
            _id = -1;
            _statusInHand = Status_ToAct;
            _lastAction = Action_Fold;
            _prevStreetAction = Action_Fold;
            _preFlopPosition = Position_Middle1;
            _stack = 0;
            _moneyInPot = 0;
        }

        explicit Player(const PlayerDTO& dto)
        {
            _id = dto._id;
            _statusInHand = dto._statusInHand;
            _lastAction = dto._lastAction;
            _prevStreetAction = dto._prevStreetAction;
            _preFlopPosition = dto._preFlopPosition;
            _stack = dto._stack;
            _moneyInPot = dto._moneyInPot;

            _model = std::make_shared<PlayerModel>(dto._model);
            _range = std::make_shared<Range>(dto._range);
        }

        explicit Player(const Player& player)
            :   _range(player._range)
            ,   _model(player._model)
        {
            _statusInHand = player._statusInHand;
            _lastAction = player._lastAction;
            _prevStreetAction = player._prevStreetAction;
            _preFlopPosition = player._preFlopPosition;
            _stack = player._stack;
            _moneyInPot = player._moneyInPot;
        }

        // Getters

        const Range& range() const
        {
            return *_range.get();
        }

        int stack() const
        {
            return _stack;
        }

        int moneyInPot() const 
        {
            return _moneyInPot;
        }

        Status statusInHand() const 
        {
            return _statusInHand;
        }

        ActionType lastAction() const
        {
            return _lastAction;
        }

        Position preFlopPosition() const 
        {
            return _preFlopPosition;
        }

        ActionType prevStreetAction() const 
        {
            return _prevStreetAction;
        }

        // Status changing functions

        void bringsIn(int amount)
        {
            _stack += amount;
        }

        void posts(int amount)
        {
            assert(_statusInHand == Status_ToAct);
            _statusInHand = Status_ToAct;
            _moneyInPot += amount;
            _stack -= amount;
        }

        void folds()
        {
            assert(_statusInHand == Status_ToAct);
            _statusInHand = Status_Folded;
            _lastAction = Action_Fold;
        }

        void checks()
        {
            assert(_statusInHand == Status_ToAct);
            _statusInHand = Status_Acted;
            _lastAction = Action_Check;
        }

        void calls(int amount)
        {
            assert(_statusInHand == Status_ToAct);
            _statusInHand = Status_Acted;
            _lastAction = Action_Call;

            _moneyInPot += amount;
            _stack -= amount;
        }

        void betsOrRaisesTo(int toAmount)
        {
            assert(_statusInHand == Status_ToAct);
            _statusInHand = Status_Acted;
            _lastAction = Action_Raise;

            int amount = toAmount - _moneyInPot;
            _moneyInPot = toAmount;
            _stack -= amount;

            assert(amount > 0);
        }

        void goesAllIn()
        {
            goesAllIn(_stack);
        }

        void goesAllIn(int amount)
        {
            assert(_statusInHand == Status_ToAct);
            _statusInHand = Status_WentAllIn;
            _lastAction = Action_AllIn;

            _moneyInPot += amount;
            _stack = 0;

            assert(amount > 0);
        }

        void nextStreet()
        {
            _prevStreetAction = _lastAction;
            _lastAction = Action_Fold;
        }

        int gound() const
        {
            return (_lastAction == Action_Fold) ? 0 : 1;
        }

        void setStatus(Status status)
        {
            _statusInHand = status;
        }

        void cutRange_CheckBet(ActionType actionType, const Board& board, float betRaiseProb, const GameContext& gc)
        {
            assert(actionType == Action_Check || actionType == Action_Bet || actionType == Action_AllIn);
            _range = _range->cutCheckBet(actionType, board, betRaiseProb, gc);
        }

        void cutRange_FoldCallRaise(ActionType actionType, const Board& board, float betRaiseProb, float checkCallProb, const GameContext& gc)
        {
            assert(actionType == Action_Call || actionType == Action_Raise || actionType == Action_AllIn);
            _range = _range->cutFoldCallRaise(actionType, board, betRaiseProb, checkCallProb, gc);
        }

        void predictAction_CheckBet(float& toCheck, float& toBet, const Board& board, float betChance, const GameContext& gc) const
        {
            _range->predictAction_CheckBet(toCheck, toBet, board, betChance, gc);
        }

        void predictAction_FoldCallRaise(float& toFold, float& toCall, float& toRaise, const Board& board, float raiseChance, float callChance, const GameContext& gc) const
        {
            _range->predictAction_FoldCallRaise(toFold, toCall, toRaise, board, raiseChance, callChance, gc);
        }

        void banCardInRange(const Card& card)
        {
            _range = _range->banCard(card);
        }

        ActionDistribution getAD(const PreFlopParams& preFlopParams) const
        {
            return _model->getAD(preFlopParams);
        }

        ActionDistribution getAD(const PostFlopParams& postFlopParams) const
        {
            return _model->getAD(postFlopParams);
        }

        void range_FillHandIndices(int* indices, int nIndices) const
        {
            _range->fillHandIndices(indices, nIndices);
        }
    };
}
