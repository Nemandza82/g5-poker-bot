using G5.Logic;
using System;
using System.Collections.Generic;
using System.Text;


namespace G5Gym
{
    class GymGame : IDisposable
    {
        private BotGameState[] _botGameStates;
        private Deck _deck = new Deck();

        private int _bigBlindSize;
        private int _startStackSize;
        private int _numPlayers;
        private bool _isFakeHand;

        private bool[] _controlPlayer;
        private OpponentModeling _opponentModeling;

        public GymGame(TableType tableType, bool[] controlPlayer)
        {
            _bigBlindSize = 100;
            _startStackSize = 200 * _bigBlindSize;

            if (tableType == TableType.HeadsUp)
            {
                _numPlayers = 2;
            }
            else if (tableType == TableType.SixMax)
            {
                _numPlayers = 6;
                throw new InvalidOperationException("Not supported table type SixMax");
            }
            else
            {
                throw new InvalidOperationException("Not supported table type");
            }

            String[] playerNames = { "Player0", "Player1" };
            _botGameStates = new BotGameState[_numPlayers];

            _controlPlayer = controlPlayer;

            var oppModelingOptions = new OpponentModeling.Options();
            oppModelingOptions.recentHandsCount = 1000;

            for (int i = 0; i < _numPlayers; i++)
            {
                Console.WriteLine($"Player {i} is controlled by gym {controlPlayer[i]}");

                Console.WriteLine($"Loading _opponentModeling for player {i}");
                _opponentModeling = controlPlayer[i] ? new OpponentModeling("full_stats_list_hu.bin", 
                    // _bigBlindSize,
                    tableType,
                    oppModelingOptions) : null;
                
                Console.WriteLine("OpponentModeling loaded successfully");

                Console.WriteLine("Creating estimator");
                var estimator = controlPlayer[i] ? new G5.Logic.Estimators.ModelingEstimator(_opponentModeling, PokerClient.G5) : null;

                Console.WriteLine("Creating bot game state");

                int[] stackSizesHU = { _startStackSize, _startStackSize };
                _botGameStates[i] = new BotGameState(playerNames, stackSizesHU, i, 0, _bigBlindSize, PokerClient.G5, tableType, estimator);
            }
        }

        public void Dispose()
        {
            foreach (var botGameState in _botGameStates)
                botGameState.Dispose();

            _botGameStates = null;
        }

        private List<Player> getPlayers()
        {
            return _botGameStates[0].getPlayers();
        }

        private dynamic finishHand()
        {
            var handStrengths = new List<int>();

            if (_botGameStates[0].getBoard().Count == 5)
            {
                foreach (var botGameState in _botGameStates)
                {
                    var handStrength = botGameState.calculateHeroHandStrength();
                    handStrengths.Add(handStrength.Value());

                    Console.WriteLine(botGameState.getHero().Name + " " + botGameState.getHero().StatusInHand +
                        " (" + botGameState.getHeroHoleCards() + "): " + handStrength);
                }
            }
            else
            {
                foreach (var botGameState in _botGameStates)
                    handStrengths.Add(0);
            }

            var winnings = Pot.calculateWinnings(getPlayers(), handStrengths);

            for (int i = 0; i < winnings.Count; i++)
            {
                if (winnings[i] > 0)
                    Console.WriteLine(getPlayers()[i].Name + " wins: " + winnings[i]);
            }

            foreach (var botGameState in _botGameStates)
                botGameState.finishHand(winnings);

            // Update opponent modeling...
            if (!_isFakeHand)
            {
                var startTime = DateTime.Now;
                _opponentModeling?.addHand(_botGameStates[0].getCurrentHand());
                Console.WriteLine($"Opponent modelling added hand [{DateTime.Now - startTime}]");
            }

            var saldo = new int[_numPlayers];

            for (int i = 0; i < _numPlayers; i++)
                saldo[i] = getPlayers()[i].Stack - _startStackSize;

            return new { status = "hand_finished", winnings = winnings, saldo=saldo };
        }

        private dynamic returnGameState(string status)
        {
            int playerToActInd = _botGameStates[0].getPlayerToActInd();

            var hc = _botGameStates[playerToActInd].getHeroHoleCards();

            dynamic holeCards = new { card0 = hc.Card0.ToRankSuite(), card1 = hc.Card1.ToRankSuite() };
            var board = _botGameStates[0].getBoard().Cards.ConvertAll((card) => card.ToRankSuite());

            while (board.Count < 5)
                board.Add(new { rank = -1, suite = -1 });

            var actionsToRet = new List<dynamic>();
            var actionList = _botGameStates[0].getCurrentHand().ActionList;
            int i = 0;

            for (var street = Street.PreFlop; street <= Street.River; street++)
            {
                int j = 0;

                while (actionList.Count > i && actionList[i].Street == street)
                {
                    actionsToRet.Add(actionList[i].toDynamic());
                    i++;
                    j++;
                }

                while (j < 5)
                {
                    actionsToRet.Add(new { type = "none", ammount = 0 });
                    j++;
                }
            }

            return new {
                status = status,
                player_to_act = playerToActInd,
                other_player_ind = (playerToActInd + 1) % _numPlayers,
                pot_size = _botGameStates[playerToActInd].potSize(),
                ammount_to_call = _botGameStates[playerToActInd].getAmountToCall(),
                stack_size = getPlayers()[playerToActInd].Stack,
                start_stack_size = _startStackSize,
                hole_cards = holeCards,
                board = board,
                actions = actionsToRet };
        }

        public dynamic startNewHand()
        {
            _isFakeHand = false;

            foreach (var botGameState in _botGameStates)
            {
                foreach (var player in botGameState.getPlayers())
                    player.SetStackSize(_startStackSize);
            }

            _deck.reset();

            Console.WriteLine("Started new hand - players are reverted to start stack size and are posting blinds");

            foreach (var botGameState in _botGameStates)
            {
                botGameState.startNewHand();
                botGameState.dealHoleCards(_deck.dealCard(), _deck.dealCard());

                Console.WriteLine("Dealing hole cards to " + botGameState.getHero().Name + " " + botGameState.getHeroHoleCards().ToString());
            }

            foreach (var player in getPlayers())
                Console.WriteLine(player.Name + " (" + player.PreFlopPosition + ") stack: " + player.Stack.ToString());

            // Maybe gym is controling first player...
            return playControledPlayers();
        }

        private void nextStreet(Street street)
        {
            int numCardsToDraw = (street == Street.PreFlop) ? 3 : 1;
            List<Card> cards = new List<Card>();
            var cardStr = "";

            for (int i = 0; i < numCardsToDraw; i++)
            {
                var card = _deck.dealCard();
                cards.Add(card);
                cardStr += card.ToString();
            }

            Console.WriteLine("Dealing " + (street + 1).ToString() + " " + cardStr);

            foreach (var botGameState in _botGameStates)
                botGameState.goToNextStreet(cards);
        }

        private dynamic playControledPlayers()
        {
            var playerToAct = _botGameStates[0].getPlayerToActInd();

            if (playerToAct < 0)
            {
                Console.WriteLine($"Error: Hand Finished !!!!!!!!!!!");
                return returnGameState("action_expected");
            }

            if (!_controlPlayer[playerToAct])
                return returnGameState("action_expected");

            Console.WriteLine($"Gym: Playing controled player: {playerToAct}; Calculating action.");

            var bd = _botGameStates[playerToAct].calculateHeroAction();
            return playerActs(bd.actionType, bd.byAmmount, false);
        }

        public dynamic playerActs(ActionType actionType, int byAmmount, bool isFakeAction)
        {
            if (isFakeAction)
                _isFakeHand = true;

            foreach (var botGameState in _botGameStates)
                botGameState.playerActs(actionType, byAmmount);

            // Maybe go to next street...
            while (_botGameStates[0].getPlayerToActInd() < 0)
            {
                Street street = _botGameStates[0].getStreet();

                if (street == Street.River)
                {
                    return finishHand();
                }
                else if (_botGameStates[0].numActivePlayers() >= 2)
                {
                    // Go to next street...
                    nextStreet(street);
                }
                else
                {
                    return finishHand();
                }
            }

            // Maybe now we are controling player...
            return playControledPlayers();
        }
    }
}
