using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using G5.Logic;
using System.Diagnostics;
using System.IO;

namespace G5.PlayVsBots
{
    public partial class PlayVsBotsForm : Form
    {
        private static readonly int NUM_PLAYERS = 6;

        private OpponentModeling.Options _options;
        private OpponentModeling _opponentModeling;
        private BotGameState[] _botGameStates = new BotGameState[NUM_PLAYERS];
        private Deck _deck = new Deck();

        private TableType _tableType;
        private int _heroInd;
        private int _bigBlindSize;
        private int _startStackSize;
        private int _totalInvestedMoney;

        public PlayVsBotsForm()
        {
            InitializeComponent();

            _bigBlindSize = 4;
            _options = new OpponentModeling.Options();
            _options.recentHandsCount = 30;

            _tableType = (NUM_PLAYERS == 2) ? TableType.HeadsUp : TableType.SixMax;
            var statListFile = (NUM_PLAYERS == 2) ? "full_stats_list_hu.bin" : "full_stats_list_6max.bin";

            _opponentModeling = new OpponentModeling(statListFile, /*_bigBlindSize,*/ _tableType, _options);

            _gameTableControl.NextButtonPressed += buttonNext_Click;
            _gameTableControl.FoldButtonPressed += buttonFold_Click;
            _gameTableControl.CallButtonPressed += buttonCheckCall_Click;
            _gameTableControl.RaiseButtonPressed += buttonBetRaise_Click;

            _heroInd = 0;
            _startStackSize = _bigBlindSize * 100;

            String[] playerNames = new string[NUM_PLAYERS];
            int[] stackSizes = new int[NUM_PLAYERS];
            playerNames[0] = "Player";

            for (int i = 1; i < NUM_PLAYERS; i++)
            {
                playerNames[i] = "Bot" + i.ToString();
                stackSizes[i] = _startStackSize;
            }

            for (int i = 0; i < NUM_PLAYERS; i++)
            {
                _botGameStates[i] = new BotGameState(playerNames, stackSizes, i, 0, _bigBlindSize, PokerClient.G5, _tableType,
                    new Logic.Estimators.ModelingEstimator(_opponentModeling, PokerClient.G5));
            }

            _totalInvestedMoney = _startStackSize;
            startNewHand();
            displayState();
        }

        private string moneyToString(int money)
        {
            return "$" + (money / 100.0f).ToString("f2");
        }

        private List<Player> getPlayers()
        {
            return _botGameStates[_heroInd].getPlayers();
        }

        private void displayState()
        {
            int heroStack = getPlayers()[_heroInd].Stack;
            this.Text = "Play vs Bots (" + moneyToString(heroStack - _totalInvestedMoney) + ")";

            int playerToActInd = _botGameStates[_heroInd].getPlayerToActInd();
            int buttonInd = _botGameStates[_heroInd].getButtonInd();

            for (int i = 0; i < 6; i++)
                _gameTableControl.hidePlayerInfo(i);

            for (int i = 0; i < NUM_PLAYERS; i++)
            {
                HoleCards hh = null;
                var street = _botGameStates[_heroInd].getStreet();

                if (i == _heroInd ||
                    playerToActInd < 0 && street == Street.River ||
                    _botGameStates[_heroInd].numActiveNonAllInPlayers() == 0 /*||
                    _botGameStates[_heroInd].getPlayers()[_heroInd].StatusInHand == Status.Folded*/)
                {
                    hh = _botGameStates[i].getHeroHoleCards();
                }

                Player player = getPlayers()[i];
                int wherePlayerSits = (NUM_PLAYERS == 2) ? (i * 3) : i;

                _gameTableControl.updatePlayerInfo(wherePlayerSits, player.Name, player.Stack, player.MoneyInPot, player.StatusInHand, hh,
                    player.PreFlopPosition, (playerToActInd == i));

                if (i == buttonInd)
                    _gameTableControl.setButtonPosition(wherePlayerSits);
            }
            
            _gameTableControl.disablePlayerControls();
            _gameTableControl.setbuttonNextEnabled(true);

            if (playerToActInd == _heroInd && (_botGameStates[_heroInd].numActivePlayers() >= 2))
            {
                _gameTableControl.enablePlayerControls();
                _gameTableControl.setbuttonNextEnabled(false);

                _gameTableControl.setupPlayerControls(_botGameStates[_heroInd].getNumBets(), _botGameStates[_heroInd].getAmountToCall(),
                    _botGameStates[_heroInd].getRaiseAmmount(), getPlayers()[_heroInd].Stack);
            }

            _gameTableControl.setPotSize(_botGameStates[_heroInd].potSize());
            _gameTableControl.displayBoard(_botGameStates[_heroInd].getBoard().Cards);
        }

        private void finishHand()
        {
            var handStrengths = new List<int>();

            foreach (var botGameState in _botGameStates)
            {
                if (_botGameStates[0].getBoard().Count == 5) // TODO CHeeeeeeeeeeck
                {
                    var handStrength = botGameState.calculateHeroHandStrength();
                    handStrengths.Add(handStrength.Value());
                }
                else
                {
                    handStrengths.Add(0);
                }
            }

            var winnings = Pot.calculateWinnings(getPlayers(), handStrengths);

            for (int i = 0; i < winnings.Count; i++)
            {
                if (winnings[i] > 0)
                {
                    var player = getPlayers()[i];
                    _gameTableControl.log(player.Name + " wins " + moneyToString(winnings[i]));
                }
            }

            foreach (var botGameState in _botGameStates)
                botGameState.finishHand(winnings);

            _opponentModeling.addHand(_botGameStates[0].getCurrentHand());
        }

        private void startNewHand()
        {
            for (int i = 0; i < NUM_PLAYERS; i++)
            {
                var player = getPlayers()[i];

                if (player.Stack == 0)
                {
                    foreach (var botGameState in _botGameStates)
                        botGameState.playerBringsIn(i, _startStackSize);

                    _gameTableControl.log(player.Name + " Brings in " + moneyToString(_startStackSize));

                    if (i == _heroInd)
                        _totalInvestedMoney += _startStackSize;
                }
            }

            _gameTableControl.log("Dealing new hand");
            _deck.reset();

            for (int i = 0; i < _botGameStates.Length; i++)
            {
                _botGameStates[i].startNewHand();
                _botGameStates[i].dealHoleCards(_deck.dealCard(), _deck.dealCard());
            }
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

            _gameTableControl.log("Dealing " + (street + 1).ToString() + " " + cardStr);

            foreach (var botGameState in _botGameStates)
                botGameState.goToNextStreet(cards);
        }

        private void playerCheckCalls()
        {
            foreach (var botGameState in _botGameStates)
                botGameState.playerCheckCalls();
        }

        private void playerBetRaisesBy(int ammount)
        {
            foreach (var botGameState in _botGameStates)
                botGameState.playerBetRaisesBy(ammount);
        }

        private void playerFolds()
        {
            foreach (var botGameState in _botGameStates)
                botGameState.playerFolds();
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            int playerToActInd = _botGameStates[_heroInd].getPlayerToActInd();

            if (playerToActInd < 0)
            {
                Street street = _botGameStates[_heroInd].getStreet();

                if (street == Street.River)
                {
                    finishHand();
                    startNewHand();
                }
                else if (_botGameStates[_heroInd].numActivePlayers() >= 2)
                {
                    nextStreet(street);
                }
                else
                {
                    finishHand();
                    startNewHand();
                }
            }
            else
            {
                Debug.Assert(playerToActInd != _heroInd);

                var bd = _botGameStates[playerToActInd].calculateHeroAction();

                var workTimeStr = "[" + bd.timeSpentSeconds.ToString("f2") + "s]";
                var actionStr = "";

                if (bd.actionType == ActionType.Fold)
                {
                    actionStr = " folds ";
                    playerFolds();
                }
                else if (bd.actionType == ActionType.Check || bd.actionType == ActionType.Call)
                {
                    if (bd.actionType == ActionType.Check)
                        actionStr = " checks ";

                    if (bd.actionType == ActionType.Call)
                        actionStr = " calls ";

                    playerCheckCalls();
                }
                else if (bd.actionType == ActionType.Bet || bd.actionType == ActionType.Raise || bd.actionType == ActionType.AllIn)
                {
                    if (bd.actionType == ActionType.Bet)
                        actionStr = " bets ";

                    if (bd.actionType == ActionType.Raise)
                        actionStr = " raises ";

                    if (bd.actionType == ActionType.AllIn)
                        actionStr = " goes allin ";

                    playerBetRaisesBy(bd.byAmmount);
                }

                var botName = getPlayers()[playerToActInd].Name;
                _gameTableControl.log(botName + actionStr + workTimeStr);
            }

            displayState();
        }

        private void buttonFold_Click(object sender, EventArgs e)
        {
            _gameTableControl.log(_botGameStates[_heroInd].getPlayerToAct().Name + " folds");
            playerFolds();
            displayState();
        }

        private void buttonCheckCall_Click(object sender, EventArgs e)
        {
            var actionStr = (_botGameStates[_heroInd].getAmountToCall() > 0) ? "calls" : "checks";
            _gameTableControl.log(_botGameStates[_heroInd].getPlayerToAct().Name + " " + actionStr);
            playerCheckCalls();
            displayState();
        }

        private void buttonBetRaise_Click(object sender, int raiseAmmount)
        {
            var actionStr = (_botGameStates[_heroInd].getNumBets() > 0) ? "raises" : "bets";
            _gameTableControl.log(_botGameStates[_heroInd].getPlayerToAct().Name + " " + actionStr);

            var ra = (raiseAmmount == 0) ? _botGameStates[_heroInd].getRaiseAmmount() : raiseAmmount;
            playerBetRaisesBy(ra);

            displayState();
        }
    }
}
