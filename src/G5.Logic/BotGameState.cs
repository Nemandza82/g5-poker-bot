using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace G5.Logic
{
    public class BotGameState : IDisposable
    {
        private const int RAISE_SIZE_NOM = 2;
        private const int RAISE_SIZE_DEN = 3;

        private int _bigBlingSize;
        private PokerClient _pokerClient;
        private TableType _tableType;

        private Estimators.IActionEstimator _actionEstimator;
        private List<Player> _players;
        private Board _board;
        private HoleCards _heroHoleCards;
        private Hand _currentHand;

        private int _playerToActInd;
        private int _heroInd;
        private int _buttonInd;
        private Street _street;
        private int _numBets;
        private int _numCallers;

        private int smallBlindInd()
        {
            if (_players.Count == 2)
                return _buttonInd;

            return (_buttonInd + 1) % _players.Count;
        }

        private int bigBlindInd()
        {
            if (_players.Count == 2)
                return (_buttonInd + 1) % 2;

            return (_buttonInd + 2) % _players.Count;
        }

        public BotGameState(string[] playerNames,
            int[] stackSizes,
            int heroIndex, 
            int buttonInd, 
            int bigBlingSize, 
            PokerClient client, 
            TableType tableType, 
            Estimators.IActionEstimator actionEstimator)
        {
            if (playerNames.Count() != stackSizes.Count())
                throw new Exception("Length of playerNames and stackSizes arrays must be the same");

            _actionEstimator = actionEstimator;

            _tableType = tableType;
            _players = new List<Player>();
            _board = new Board();
            _heroHoleCards = new HoleCards(0);

            _bigBlingSize = bigBlingSize;
            _pokerClient = client;
            _heroInd = heroIndex;
            _buttonInd = buttonInd;

            for (int i=0; i< playerNames.Count(); i++)
            {
                _players.Add(new Player(playerNames[i], stackSizes[i], null));
            }
        }

        public void startNewHand()
        {
            _street = Street.PreFlop;
            _numBets = 0;
            _numCallers = 0;

            _actionEstimator.newHand(this);

            foreach (Player player in _players)
                player.ResetHand();

            Debug.Assert(_players.Count >= 2);

            _players[_buttonInd].PreFlopPosition = Position.Button;
            _players[smallBlindInd()].PreFlopPosition = Position.SmallBlind;
            _players[bigBlindInd()].PreFlopPosition = Position.BigBlind;

            if (_players.Count > 3)
            {
                int cutoffInd = (_buttonInd + _players.Count - 1) % _players.Count;
                _players[cutoffInd].PreFlopPosition = Position.CutOff;
            }

            if (_players.Count > 4)
            {
                int middleInd = (_buttonInd + _players.Count - 2) % _players.Count;
                _players[middleInd].PreFlopPosition = Position.Middle2;
            }

            if (_players.Count > 5)
            {
                int middleInd = (_buttonInd + _players.Count - 3) % _players.Count;
                _players[middleInd].PreFlopPosition = Position.Middle1;
            }

            _playerToActInd = (_players.Count > 2) ? ((_buttonInd + 3) % _players.Count) : smallBlindInd();

            _currentHand = new Hand
            {
                Client = _pokerClient,
                GameType = GameType.HoldEm,
                BigBlindSize = _bigBlingSize,
                HeroName = _players[_heroInd].Name
            };

            if (_players.Count == 2)
            {
                _currentHand.addPlayer(_players[smallBlindInd()].Name, _players[smallBlindInd()].Stack);
                _currentHand.addPlayer(_players[bigBlindInd()].Name, _players[bigBlindInd()].Stack);
            }
            else
            {
                for (int i = 1; i <= _players.Count; i++)
                {
                    int ind = (_buttonInd + i) % _players.Count;
                    _currentHand.addPlayer(_players[ind].Name, _players[ind].Stack);
                }
            }

            _board = new Board();
            _players[smallBlindInd()].Posts(_bigBlingSize / 2);
            _players[bigBlindInd()].Posts(_bigBlingSize);
        }

        public void finishHand(List<int> winnings)
        {
            Debug.Assert(_players.Count == winnings.Count);

            for (int i = 0; i < winnings.Count; i++)
            {
                if (winnings[i] > 0)
                {
                    _currentHand.addAction(_street, _players[i].Name, ActionType.Wins, winnings[i]);
                    _players[i].WinsHand(winnings[i]);
                }
            }

            var playerSaldo = new Dictionary<string, int>();

            foreach (var player in _players)
                playerSaldo[player.Name] = player.MoneyWon - player.MoneyInPot;

            _currentHand.addPlayerWinnings(playerSaldo);
            _buttonInd = (_buttonInd + 1) % _players.Count;
        }

        public TableType getTableType()
        {
            return _tableType;
        }

        public int getBigBlingSize()
        {
            return _bigBlingSize;
        }

        public Hand getCurrentHand()
        {
            return _currentHand;
        }

        public Street getStreet()
        {
            return _street;
        }

        public int getPlayerToActInd()
        {
            return _playerToActInd;
        }

        public int getButtonInd()
        {
            return _buttonInd;
        }

        public List<Player> getPlayers()
        {
            return _players;
        }

        public Player getHero()
        {
            return _players[_heroInd];
        }

        public int getHeroInd()
        {
            return _heroInd;
        }

        public HoleCards getHeroHoleCards()
        {
            return _heroHoleCards;
        }

        public Board getBoard()
        {
            return _board;
        }

        /// <summary>
        ///  Calculates the hand strength of the hero cards.
        /// </summary>
        public HandStrength calculateHeroHandStrength()
        {
            return HandStrength.calculateHandStrength(_heroHoleCards, _board);
        }

        public int getNumBets()
        {
            return _numBets;
        }

        public int getNumCallers()
        {
            return _numCallers;
        }

        public int potSize()
        {
            int sum = 0;

            foreach (var player in _players)
                sum += player.MoneyInPot;

            return sum;
        }

        public int getMaxMoneyInThePot()
        {
            int max = 0;

            foreach (var player in _players)
            {
                if (max < player.MoneyInPot)
                    max = player.MoneyInPot;
            }

            return max;
        }

        public int getRaiseAmmount()
        {
            int ammountToCall = getMaxMoneyInThePot() - getPlayerToAct().MoneyInPot;

            if (_street == Street.PreFlop)
            {
                return potSize() + 2 * ammountToCall;
            }
            else
            {
                return (RAISE_SIZE_NOM * (potSize() + ammountToCall)) / RAISE_SIZE_DEN + ammountToCall;
            }
        }

        public int getAmountToCall()
        {
            int amountToCall = getMaxMoneyInThePot() - getPlayerToAct().MoneyInPot;
            int playerToActStack = getPlayerToAct().Stack;

            if (amountToCall >= playerToActStack)
                amountToCall = playerToActStack;

            return amountToCall;
        }

        public Player getPlayerToAct()
        {
            return _players[_playerToActInd];
        }

        public int numActivePlayers()
        {
            int activePlayers = 0;

            foreach (var player in _players)
            {
                if (player.StatusInHand != Status.Folded)
                    activePlayers++;
            }

            return activePlayers;
        }

        public int numActiveNonAllInPlayers()
        {
            int activePlayers = 0;

            foreach (var player in _players)
            {
                if (player.StatusInHand != Status.Folded && player.StatusInHand != Status.AllIn)
                    activePlayers++;
            }

            return activePlayers;
        }

        private float getPossibleWinnings(int bigBlindSize)
        {
            int winnings = 0;
            int maxOppMoneyInThePot = 0;

            for (int i = 0; i < _players.Count; i++)
            {
                winnings += Math.Min(_players[i].MoneyInPot, _players[_heroInd].MoneyInPot);

                if (i != _heroInd)
                {
                    maxOppMoneyInThePot = Math.Max(maxOppMoneyInThePot, _players[i].MoneyInPot);
                }
            }

            int rakableWinnings = winnings - Math.Max(_players[_heroInd].MoneyInPot - maxOppMoneyInThePot, 0);
            int rake;

            if (bigBlindSize <= 10)
            {
                rake = rakableWinnings / 10;
                rake = Math.Min(rake, 10);
            }
            else
            {
                rake = rakableWinnings / 15;
            }

            winnings -= rake;
            return (float)winnings;
        }

        public void playerBringsIn(int playerInd, int ammont)
        {
            _players[playerInd].BringsIn(ammont);
        }

        public void dealHoleCards(HoleCards holeCards)
        {
            dealHoleCards(holeCards.Card0, holeCards.Card1);
        }

        public void dealHoleCards(Card card0, Card card1)
        {
            Debug.Assert(_street == Street.PreFlop);
            _heroHoleCards = new HoleCards(card0, card1);
            _currentHand.setHoleCardsHoldem(card0, card1);

            for (int i = 0; i < _players.Count; i++)
            {
                if (i != _heroInd)
                {
                    _players[i].BanCardInRange(card0, false);
                    _players[i].BanCardInRange(card1, false);
                }
            }
        }

        private void goToFirstPlayer()
        {
            if (numActivePlayers() <= 1)
            {
                _playerToActInd = -1;
                return;
            }

            for (int i = 1; i <= _players.Count; i++)
            {
                int ind = (_buttonInd + i) % _players.Count;

                if (_players[ind].StatusInHand == Status.ToAct)
                {
                    _playerToActInd = ind;
                    return;
                }
            }

            _playerToActInd = -1;
        }

        public void goToNextStreet(Card card)
        {
            goToNextStreet(new List<Card>(new Card[] { card }));
        }

        public void goToNextStreet(List<Card> cards)
        {
            Debug.Assert(_street == Street.PreFlop ||
                         _street == Street.Flop ||
                         _street == Street.Turn);

            if (_street == Street.PreFlop)
                Debug.Assert(cards.Count == 3);
            else
                Debug.Assert(cards.Count == 1);

            // Reset acted players after street
            int acted = 0;
            int allin = 0;

            foreach (var player in _players)
            {
                if (player.StatusInHand == Status.Acted)
                    acted++;

                if (player.StatusInHand == Status.AllIn)
                    allin++;
            }

            // Svi su AllIn osim jednog koji ima vise novca - On postaje AllIn takodje.
            if (acted == 1 && allin > 0)
            {
                foreach (var player in _players)
                {
                    if (player.StatusInHand == Status.Acted)
                        player.StatusInHand = Status.AllIn;
                }
            }

            foreach (var player in _players)
            {
                if (player.StatusInHand == Status.Acted)
                    player.StatusInHand = Status.ToAct;
            }

            // Players go to next street
            foreach (var player in _players)
            {
                player.NextStreet();

                foreach (Card card in cards)
                    player.BanCardInRange(card, true);
            }

            // Add card to board
            foreach (Card card in cards)
            {
                _currentHand.Board.AddCard(card);
                _board.AddCard(card);
            }

            // Set other parameters
            goToFirstPlayer();
            _street = _street + 1;
            _numBets = 0;
            _numCallers = 0;

            if (_street == Street.Flop)
                _actionEstimator.flopShown(getBoard(), getHeroHoleCards());
        }

        private void goToNextPlayer()
        {
            if (numActivePlayers() <= 1)
            {
                _playerToActInd = -1;
                return;
            }

            for (int i = _playerToActInd + 1; i < _players.Count; i++)
            {
                if (_players[i].StatusInHand == Status.ToAct)
                {
                    _playerToActInd = i;
                    return;
                }
            }

            for (int i = 0; i < _playerToActInd; i++)
            {
                if (_players[i].StatusInHand == Status.ToAct)
                {
                    _playerToActInd = i;
                    return;
                }
            }

            _playerToActInd = -1;
        }

        public bool isPlayerInPosition(int playerIndex)
        {
            // Button is in position in each situation
            if (playerIndex == _buttonInd)
                return true;

            // For each opponent up to button
            for (int i = 1; i < _players.Count; i++)
            {
                int oppInd = (playerIndex + i) % _players.Count;
                Debug.Assert(oppInd != playerIndex);

                if (_players[oppInd].StatusInHand == Status.ToAct || _players[oppInd].StatusInHand == Status.Acted)
                    return false;

                if (oppInd == _buttonInd)
                    break;
            }

            return true;
        }

        public ActionType playerActs(ActionType actionType, int byAmmount)
        {
            if (actionType == ActionType.Fold)
            {
                playerFolds();
                return ActionType.Fold;
            }
            else if (actionType == ActionType.Check || actionType == ActionType.Call)
            {
                return playerCheckCalls();
            }
            else
            {
                return playerBetRaisesBy(byAmmount);
            }
        }

        public ActionType playerCheckCalls()
        {
            int ammountToCall = getAmountToCall();
            ActionType actionType = ActionType.Check;

            if (ammountToCall >= getPlayerToAct().Stack)
            {
                // Important that this is before player state changes (getPlayerToAct().GoesAllIn(...))
                _actionEstimator.newAction(ActionType.Call, this);

                ammountToCall = getPlayerToAct().Stack;
                getPlayerToAct().GoesAllIn();
                actionType = ActionType.AllIn;
                _numCallers++;
            }
            else if (ammountToCall > 0) // Ima betova
            {
                // Important that this is before player state changes (getPlayerToAct().Calls(...))
                _actionEstimator.newAction(ActionType.Call, this);

                getPlayerToAct().Calls(ammountToCall);
                actionType = ActionType.Call;
                _numCallers++;
            }
            else
            {
                // Important that this is before player state changes (getPlayerToAct().Checks())
                _actionEstimator.newAction(ActionType.Check, this);

                getPlayerToAct().Checks();
                actionType = ActionType.Check;
            }

            Console.WriteLine($"{getPlayerToAct().Name} checks/calls: {ammountToCall}");
            _currentHand.addAction(_street, getPlayerToAct().Name, actionType, ammountToCall);

            goToNextPlayer();
            return actionType;
        }

        private void resetActedPlayersAfterRaise()
        {
            int newAmountInPot = getMaxMoneyInThePot();

            foreach (var player in _players)
            {
                if (player.MoneyInPot < newAmountInPot && player.StatusInHand == Status.Acted)
                    player.StatusInHand = Status.ToAct;
            }
        }

        public ActionType playerBetRaisesBy(int ammount)
        {
            ActionType actionType = ActionType.Fold;
            var betOrRaise = (getAmountToCall() == 0) ? ActionType.Bet : ActionType.Raise;

            if (ammount >= getPlayerToAct().Stack)
            {
                // Important that this is before player state changes (getPlayerToAct().GoesAllIn())
                _actionEstimator.newAction(betOrRaise, this);

                ammount = getPlayerToAct().Stack;
                getPlayerToAct().GoesAllIn();
                actionType = ActionType.AllIn;
            }
            else
            {
                // Important that this is before player state changes (getPlayerToAct().BetsOrRaisesTo(...))
                _actionEstimator.newAction(betOrRaise, this);

                getPlayerToAct().BetsOrRaisesTo(getPlayerToAct().MoneyInPot + ammount);
                actionType = betOrRaise;
            }

            Console.WriteLine($"{getPlayerToAct().Name} bets/raises by: {ammount}");
            _currentHand.addAction(_street, getPlayerToAct().Name, actionType, ammount);

            resetActedPlayersAfterRaise();
            goToNextPlayer();
            _numCallers = 0;
            _numBets += 1;

            return actionType;
        }

        public void playerFolds()
        {
            Console.WriteLine(getPlayerToAct().Name + " folds");
            _currentHand.addAction(_street, getPlayerToAct().Name, ActionType.Fold, 0);

            getPlayerToAct().Folds();
            goToNextPlayer();
        }

        public struct BotDecision
        {
            public ActionType actionType;
            public int byAmmount;
            public float checkCallEV;
            public float betRaiseEV;
            public double timeSpentSeconds;
        }

        public BotDecision calculateHeroAction()
        {
            int nOfOpponents = numActivePlayers() - 1;

            Debug.Assert(_playerToActInd == _heroInd);
            Debug.Assert(nOfOpponents > 0);

            BotDecision bd = new BotDecision
            {
                actionType = ActionType.Fold,
                byAmmount = 0,
                betRaiseEV = 0.0f,
                checkCallEV = 0.0f,
                timeSpentSeconds = 0
            };

            var startTime = DateTime.Now;
            int ammountToCall = getAmountToCall();

            if (nOfOpponents < 4 || _street == Street.PreFlop)
            {
                _actionEstimator.estimateEV(out bd.checkCallEV, out bd.betRaiseEV, this);
            }
            else
            {
                // If ammountToCall is 0, it can check
                bd.checkCallEV = -ammountToCall;
                bd.betRaiseEV = -10.0f;
            }

            bd.timeSpentSeconds = (DateTime.Now - startTime).TotalSeconds;
            bd.actionType = ActionType.Fold;
            bd.byAmmount = 0;

            // If both EVs are less than zero then fold
            if (bd.checkCallEV < 0 && bd.betRaiseEV <= 0)
            {
                if (_street > Street.PreFlop)
                    Debug.Assert(_numBets > 0);

                bd.actionType = ActionType.Fold;
            }
            else
            {
                if (ammountToCall >= _players[_heroInd].Stack)
                {
                    bd.byAmmount = _players[_heroInd].Stack;
                    bd.actionType = ActionType.Call;
                }
                else if (bd.betRaiseEV < 0)
                {
                    bd.byAmmount = ammountToCall;
                    bd.actionType = (ammountToCall > 0) ? ActionType.Call : ActionType.Check;
                }
                else if (bd.betRaiseEV > bd.checkCallEV)
                {
                    bd.actionType = ActionType.Raise;
                    bd.byAmmount = getRaiseAmmount();

                    if ((3 * bd.byAmmount / 2) >= getPlayerToAct().Stack)
                    {
                        bd.byAmmount = getPlayerToAct().Stack;
                        bd.actionType = ActionType.AllIn;
                    }
                }
                else
                {
                    bd.actionType = (ammountToCall > 0) ? ActionType.Call : ActionType.Check;
                    bd.byAmmount = ammountToCall;
                }

                /* // Random actions
                if (_random.NextDouble() < checkCallEV * checkCallEV / (checkCallEV * checkCallEV + betRaiseEV * betRaiseEV))
                {
                    return (_numBets == 0) ? ActionType.Check : ActionType.Call;
                }
                else
                {
                    return ActionType.Raise;
                }*/
            }

            return bd;
        }

        public void Dispose()
        {
            _actionEstimator.Dispose();
        }
    }
}
