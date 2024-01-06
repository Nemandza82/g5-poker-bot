using G5.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static G5.Logic.BotGameState;



namespace G5Gym
{
    public class PythonAPI : IDisposable
    {
        private OpponentModeling _opponentModeling;
        private Dictionary<string, BotGameState> _botGameStates = new Dictionary<string, BotGameState>();

        public PythonAPI(int numPlayers)
        {
            var opponentModelingOptions = new OpponentModeling.Options();
            opponentModelingOptions.recentHandsCount = 50;

            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (numPlayers == 2)
            {
                _opponentModeling = new OpponentModeling(assemblyFolder + "/full_stats_list_hu.bin", 
                    TableType.HeadsUp, 
                    opponentModelingOptions);
            }
            else if (numPlayers > 2 && numPlayers <= 6)
            {
                _opponentModeling = new OpponentModeling(assemblyFolder + "/full_stats_list_6max.bin", 
                    TableType.SixMax, 
                    opponentModelingOptions);
            }
            else
            {
                throw new InvalidOperationException("Not supported table type");
            }

            Console.WriteLine("Created opponent modelling successfully");
        }

        public dynamic testCallArray()
        {
            return (new int[] { 1, 3, 5, 7, 9 }).ToList();
        }

        public dynamic testCallStruct()
        {
            return new { Amount = 108, Message = "Hello" };
        }

        // bigBlindSize int in cents. Eg. $0.04 is 4.
        public dynamic createGame(string gameName, string[] playerNames, int[] stackSizes, int heroInd, int buttonInd, int bigBlindSize)
        {
            _botGameStates[gameName] = new BotGameState(playerNames, 
                stackSizes,
                heroInd, 
                buttonInd,
                bigBlindSize,
                PokerClient.PokerKing,
                TableType.SixMax,
                new G5.Logic.Estimators.ModelingEstimator(_opponentModeling, PokerClient.PokerKing));

            Console.WriteLine($"Created {gameName} BotGameState successfully");
            return gameName;
        }

        public int getPlayerToActInd(string gameName)
        {
            return _botGameStates[gameName].getPlayerToActInd();
        }

        public int getAmountToCall(string gameName)
        {
            return _botGameStates[gameName].getAmountToCall();
        }

        public dynamic getStackSize(string gameName, int playerIndex)
        {
            var players = _botGameStates[gameName].getPlayers();

            if (playerIndex < 0 || playerIndex >= players.Count)
                return 0;

            return players[playerIndex].Stack;
        }

        public void setStackSize(string gameName, int playerIndex, int stackSize)
        {
            var players = _botGameStates[gameName].getPlayers();

            if (playerIndex < 0 || playerIndex >= players.Count)
                return;

            players[playerIndex].SetStackSize(stackSize);
        }

        public dynamic getStakesSize(string gameName, int playerIndex)
        {
            var players = _botGameStates[gameName].getPlayers();

            if (playerIndex < 0 || playerIndex >= players.Count)
                return 0;

            return players[playerIndex].MoneyInPot;
        }

        public dynamic getPlayerName(string gameName, int playerIndex)
        {
            var players = _botGameStates[gameName].getPlayers();

            if (playerIndex < 0 || playerIndex >= players.Count)
                return "Unknows player index";

            return players[playerIndex].Name;
        }

        public void setPlayerName(string gameName, int playerIndex, string playerName)
        {
            var players = _botGameStates[gameName].getPlayers();

            if (playerIndex < 0 || playerIndex >= players.Count)
                return;

            players[playerIndex].SetPlayerName(playerName);
        }

        public dynamic isPlayerInIngame(string gameName, int playerIndex)
        {
            var players = _botGameStates[gameName].getPlayers();

            if (playerIndex < 0 || playerIndex >= players.Count)
                return false;

            return players[playerIndex].StatusInHand != Status.Folded;
        }

        public dynamic getBoard(string gameName)
        {
            var board = _botGameStates[gameName].getBoard().Cards;
            var result = new List<string>();

            foreach (var card in board)
            {
                result.Add(card.ToString());
            }

            return result;
        }

        public dynamic getHoleCards(string gameName)
        {
            var holecards = _botGameStates[gameName].getHeroHoleCards();

            return new List<string>
            {
                holecards.Card0.ToString(),
                holecards.Card1.ToString()
            };
        }

        public dynamic getButtonInd(string gameName)
        {
            return _botGameStates[gameName].getButtonInd();
        }

        public dynamic getPotSize(string gameName)
        {
            return _botGameStates[gameName].potSize();
        }

        public void startNewHand(string gameName, int buttonInd)
        {
            _botGameStates[gameName].setButtonInd(buttonInd);
            _botGameStates[gameName].startNewHand();
        }

        public void dealHoleCards(string gameName, string card0, string card1)
        {
            _botGameStates[gameName].dealHoleCards(new Card(card0), new Card(card1));
        }

        public void goToFlop(string gameName, string card0, string card1, string card2)
        {
            List<Card> cards = [new Card(card0), new Card(card1), new Card(card2)];
            _botGameStates[gameName].goToNextStreet(cards);
        }

        public void goToTurn(string gameName, string card)
        {
            _botGameStates[gameName].goToNextStreet(new Card(card));
        }

        public void goToRiver(string gameName, string card)
        {
            _botGameStates[gameName].goToNextStreet(new Card(card));
        }

        public void playerCheckCalls(string gameName)
        {
            _botGameStates[gameName].playerCheckCalls();
        }

        public void playerBetRaisesBy(string gameName, int amount)
        {
            _botGameStates[gameName].playerBetRaisesBy(amount);
        }

        public void playerFolds(string gameName)
        {
            _botGameStates[gameName].playerFolds();
        }

        public dynamic calculateHeroAction(string gameName)
        {
            var bd = _botGameStates[gameName].calculateHeroAction();

            return new {
                actionType = bd.actionType,
                byAmount = bd.byAmount,
                checkCallEV = bd.checkCallEV,
                betRaiseEV = bd.betRaiseEV,
                timeSpentSeconds = bd.timeSpentSeconds,
                message = bd.message
            };
        }

        public void finishHand(string gameName, int winnings0, int winnings1, int winnings2, int winnings3, int winnings4, int winnings5)
        {
            var winnings = new List<int> { 
                winnings0, 
                winnings1, 
                winnings2,
                winnings3,
                winnings4,
                winnings5,
            };

            _botGameStates[gameName].finishHand(winnings);
            _opponentModeling.addHand(_botGameStates[gameName].getCurrentHand());
        }

        public void Dispose()
        {
            foreach (var botGameState in _botGameStates)
            {
                botGameState.Value.Dispose();
            }
        }
    }
}
