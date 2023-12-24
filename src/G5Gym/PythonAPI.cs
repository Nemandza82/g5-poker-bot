using G5.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;



namespace G5Gym
{
    public class PythonAPI : IDisposable
    {
        private OpponentModeling _opponentModeling;
        private BotGameState? _botGameState; 

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
            _botGameState = null;
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
        public void createGame(string[] playerNames, int[] stackSizes, int heroInd, int buttonInd, int bigBlindSize)
        {
            _botGameState = new BotGameState(playerNames, 
                stackSizes,
                heroInd, 
                buttonInd,
                bigBlindSize,
                PokerClient.PokerKing,
                TableType.SixMax,
                new G5.Logic.Estimators.ModelingEstimator(_opponentModeling, PokerClient.PokerKing));

            Console.WriteLine($"Created BotGameState successfully");
        }

        public void startNewHand()
        {
            _botGameState.startNewHand();
        }

        public void dealHoleCards(string card0, string card1)
        {
            _botGameState.dealHoleCards(new Card(card0), new Card(card1));
        }

        public void finishHand()
        {
            // _botGameState.finishHand(List<int> winnings);
            _opponentModeling.addHand(_botGameState.getCurrentHand());
        }

        public void Dispose()
        {
            if (_botGameState != null)
                _botGameState.Dispose();
        }
    }
}
