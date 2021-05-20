using G5.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace G5.Acpc
{
    class AcpcGame : IDisposable
    {
        private OpponentModeling.Options _options;
        private OpponentModeling _opponentModeling;
        private BotGameState _botGameState;
        private MatchState _prevMatchState;

        private TableType _tableType;
        private int _numPlayers;
        private int _heroInd;
        private int _bigBlindSize;
        private int _startStackSize;
        private int _totalSaldo;

        public AcpcGame(TableType tableType)
        {
            _botGameState = null;
            _prevMatchState = null;

            _tableType = tableType;
            _heroInd = 0;
            _bigBlindSize = 100;
            _startStackSize = 200 * _bigBlindSize;
            _totalSaldo = 0;

            _options = new OpponentModeling.Options();
            _options.recentHandsCount = 1000;

            var startTime = DateTime.Now;

            if (tableType == TableType.HeadsUp)
            {
                _numPlayers = 2;
                _opponentModeling = new OpponentModeling("full_stats_list_hu.bin", _bigBlindSize, tableType, _options);
            }
            else if (tableType == TableType.SixMax)
            {
                _numPlayers = 6;
                _opponentModeling = new OpponentModeling("full_stats_list_6max.bin", _bigBlindSize, tableType, _options);
            }
            else
            {
                throw new InvalidOperationException("Not supported table type");
            }

            var timeStr = timeToString(DateTime.Now - startTime);
            Console.WriteLine("Opponent modeling made (BBSize: " + _bigBlindSize.ToString() + ")" + timeStr);
        }

        public int TotalSaldo
        {
            get
            {
                return _totalSaldo;
            }
        }

        public void Dispose()
        {
            if (_botGameState != null)
            {
                _botGameState.Dispose();
                _botGameState = null;
            }
        }

        private Player hero()
        {
            return _botGameState.getPlayers()[_heroInd];
        }

        /// <summary>
        /// Starts the match.
        /// </summary>
        /// <param name="acpcHeroPosition">This field tells the client their position relative to the dealer button. A value of 0 indicates
        /// that for the current hand, the client is the first player after the button(the small blind in ring games, or
        /// the big blind in reverse-blind heads-up games.)</param>
        private void startTheMatch(int acpcHeroPosition)
        {
            string[] playerNamesHU = { "Hero", "Villian" };
            string[] playerNames6Max = { "Hero", "V1", "V2", "V3", "V4", "V5" };
            string[] playerNames = (_tableType == TableType.HeadsUp) ? playerNamesHU : playerNames6Max;

            int buttonInd = (_heroInd - (acpcHeroPosition + 1) + _numPlayers) % _numPlayers;
            Console.WriteLine("Match started");

            var startTime = DateTime.Now;

            _botGameState = new BotGameState(playerNames, _heroInd, buttonInd, _bigBlindSize, _startStackSize, PokerClient.Acpc, _tableType,
                new Logic.Estimators.ModelingEstimator(_opponentModeling, PokerClient.Acpc));

            var timeStr = timeToString(DateTime.Now - startTime);

            Console.WriteLine("Made bot game state (Hero ind: " + _heroInd.ToString() +
                ", Button ind: " + buttonInd.ToString() +
                ", Start stack size: " + _startStackSize.ToString() + timeStr);

            foreach (var player in _botGameState.getPlayers())
                Console.WriteLine(player.Name + " (" + player.PreFlopPosition + ") stack: " + player.Stack.ToString());
        }

        private void startNewHand(HoleCards heroHoleCards, int handNumber)
        {
            foreach (var player in _botGameState.getPlayers())
                player.SetStackSize(_startStackSize);

            var startTime = DateTime.Now;
            _botGameState.startNewHand();
            var timeStr = timeToString(DateTime.Now - startTime);

            Console.WriteLine("");
            Console.WriteLine("Started new (" + handNumber + ") hand " + timeStr);
            Console.WriteLine("Players are reverted to start stack size and are posting blinds");

            foreach (var player in _botGameState.getPlayers())
                Console.WriteLine(player.Name + " (" + player.PreFlopPosition + ") stack: " + player.Stack.ToString());

            _botGameState.dealHoleCards(heroHoleCards);
            Console.WriteLine("Dealing hole cards: " + heroHoleCards.ToString());
        }

        private void catchUpActionsFromServer(Dictionary<Street, List<MatchState.ActionInfo>> newBetting, HoleCards heroHoleCards)
        {
            Street street = _botGameState.getStreet();
            int prevNumActions = (_prevMatchState.betting.ContainsKey(street)) ? _prevMatchState.betting[street].Count : 0;
            int nextNumActions = (newBetting.ContainsKey(street)) ? newBetting[street].Count : 0;

            // CatchUp with the actions...
            for (int i = prevNumActions; i < nextNumActions; i++)
            {
                var action = newBetting[street][i];
                var playerToActName = _botGameState.getPlayerToAct().Name;

                var byAmmount = action.Amount - _botGameState.getPlayerToAct().MoneyInPot;
                _botGameState.playerActs(action.Type, byAmmount);

                var heroGuess = hero().Range.getHoleCardsProb(_prevMatchState.heroHoleCards()) * 100;
                Console.Write("Server says: " + playerToActName + " " + action.Type.ToString() + "s");

                if (playerToActName == "Hero")
                    Console.Write(" [Hole Cards Guess: " + heroGuess.ToString("f1") + "%]");

                if (action.Amount > 0)
                    Console.WriteLine(" to " + action.Amount.ToString());
                else
                    Console.WriteLine("");
            }
        }

        private void nextStreet(Street street, Board board)
        {
            var startTime = DateTime.Now;

            if (street == Street.Flop)
                _botGameState.goToNextStreet(board.Flop);

            if (street == Street.Turn)
                _botGameState.goToNextStreet(board.Turn);

            if (street == Street.River)
                _botGameState.goToNextStreet(board.River);

            Console.WriteLine("Went to next street: " + street.ToString() + " (" + board.ToString() + ")" +
                timeToString(DateTime.Now - startTime));
        }

        private string botAction()
        {
            var bd = _botGameState.calculateHeroAction();
            Console.WriteLine("Bot decision: " + bd.actionType.ToString() + timeToString(bd.timeSpentSeconds));

            if (bd.timeSpentSeconds > 1)
                Console.WriteLine("************");

            if (bd.actionType == ActionType.Fold)
                return ":f";
            else if (bd.actionType == ActionType.Check || bd.actionType == ActionType.Call)
                return ":c";
            else
                return ":r" + (_botGameState.getPlayerToAct().MoneyInPot + bd.byAmmount).ToString();
        }

        public void finishHand()
        {
            if (_prevMatchState == null)
                return;

            Console.WriteLine("Ending preveous hand");
            var handStrengths = new List<int>();

            if (_prevMatchState.board.Count == 5)
            {
                for (int i = 0; i < _botGameState.getPlayers().Count; i++)
                {
                    var player = _botGameState.getPlayers()[i];
                    var playerHoleCards = _prevMatchState.playerHoleCards(player.PreFlopPosition, _numPlayers);

                    if (playerHoleCards == null)
                    {
                        handStrengths.Add(0);
                        continue;
                    }

                    var handStrength = HandStrength.calculateHandStrength(playerHoleCards, _prevMatchState.board);
                    var holeCardsGuess = (player.Range.getHoleCardsProb(playerHoleCards) * 100).ToString("f1") + "%";

                    var plNameAndAction = "";

                    if (player.StatusInHand != Status.Folded)
                        plNameAndAction = player.Name + " shows";
                    else
                        plNameAndAction = player.Name + " folded";

                    Console.WriteLine(plNameAndAction + " (" + playerHoleCards + "): " + handStrength + " [Guess: " + holeCardsGuess + "]");

                    handStrengths.Add(handStrength.Value());
                }
            }
            else
            {
                foreach (var player in _botGameState.getPlayers())
                    handStrengths.Add(0);
            }

            var winnings = Pot.calculateWinnings(_botGameState.getPlayers(), handStrengths);
            _botGameState.finishHand(winnings);

            for (int i = 0; i < _botGameState.getPlayers().Count; i++)
            {
                if (winnings[i] > 0)
                    Console.WriteLine(_botGameState.getPlayers()[i].Name + " wins: " + winnings[i]);
            }

            int thisHandSaldo = hero().Stack - _startStackSize;
            _totalSaldo += thisHandSaldo;

            Console.WriteLine("This hand saldo: {0}, Total saldo: {1}", thisHandSaldo, _totalSaldo);

            var startTime = DateTime.Now;
            _opponentModeling.addHand(_botGameState.getCurrentHand());

            Console.WriteLine("Opponent modelling added hand " + timeToString(DateTime.Now - startTime));
        }

        private string timeToString(double timeSec)
        {
            return " [" + timeSec.ToString("f2") + "s]";
        }

        private string timeToString(TimeSpan timeSpan)
        {
            return timeToString(timeSpan.TotalSeconds);
        }

        public string acceptMessageFromServer(string message)
        {
            var matchState = MatchState.Parse(message, _numPlayers);

            Console.WriteLine("Message from server: " + message);

            if (_botGameState == null)
                startTheMatch(matchState.acpcHeroPosition());

            if (_prevMatchState == null || _prevMatchState.handNumber != matchState.handNumber)
            {
                if (_prevMatchState != null)
                    finishHand();

                startNewHand(matchState.heroHoleCards(), matchState.handNumber);
            }

            if (_prevMatchState != null)
                catchUpActionsFromServer(matchState.betting, matchState.heroHoleCards());

            while (matchState.street > _botGameState.getStreet())
                nextStreet(_botGameState.getStreet() + 1, matchState.board);

            _prevMatchState = matchState;

            string responseToServer = "";

            // Is bot to act
            if (_botGameState.getPlayerToActInd() == _heroInd)
                responseToServer = message + botAction();

            if (responseToServer != "")
                Console.WriteLine("Response to server:  " + responseToServer);

            return responseToServer;
        }
    }
}
