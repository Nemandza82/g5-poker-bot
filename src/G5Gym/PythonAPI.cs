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

        public dynamic getHandState(string gameName)
        {
            return _botGameStates[gameName].getCurrentHand();
        }

        public void setStackSize(string gameName, int playerIndex, int stackSize)
        {
            _botGameStates[gameName].getPlayers()[playerIndex].SetStackSize(stackSize);
        }

        public void setPlayerName(string gameName, int playerIndex, string playerName)
        {
            _botGameStates[gameName].getPlayers()[playerIndex].SetPlayerName(playerName);
        }

        public void startNewHand(string gameName)
        {
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

        public void playerBetRaisesBy(string gameName, int ammount)
        {
            _botGameStates[gameName].playerBetRaisesBy(ammount);
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
                byAmmount = bd.byAmmount,
                checkCallEV = bd.checkCallEV,
                betRaiseEV = bd.betRaiseEV,
                timeSpentSeconds = bd.timeSpentSeconds,
                message = bd.message
            };
        }

        public void finishHand(string gameName)
        {
            // _botGameState.finishHand(List<int> winnings);
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
