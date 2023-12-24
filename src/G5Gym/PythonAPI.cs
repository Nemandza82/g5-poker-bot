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

        public PythonAPI(int numPlayers, int bigBlindSize)
        {
            var opponentModelingOptions = new OpponentModeling.Options();
            opponentModelingOptions.recentHandsCount = 50;

            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (numPlayers == 2)
            {
                _opponentModeling = new OpponentModeling(assemblyFolder + "/full_stats_list_hu.bin", 
                    bigBlindSize, 
                    TableType.HeadsUp, 
                    opponentModelingOptions);
            }
            else if (numPlayers > 2 && numPlayers <= 6)
            {
                _opponentModeling = new OpponentModeling(assemblyFolder + "/full_stats_list_6max.bin", 
                    bigBlindSize, 
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

        public void testCall()
        {
            Console.WriteLine("G5.Gym: Loaded Successfully");
        }

        public dynamic testCallArray()
        {
            return (new int[] { 1, 3, 5, 7, 9 }).ToList();
        }

        public dynamic testCallStruct()
        {
            return new { Amount = 108, Message = "Hello" };
        }

        public dynamic newHand()
        {
            return "";
        }

        public void Dispose()
        {
            if (_botGameState != null)
                _botGameState.Dispose();
        }
    }
}
