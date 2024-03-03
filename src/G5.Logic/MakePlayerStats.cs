using G5.Logic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace G5.Logic
{
    class MakePlayerStats
    {
        /// <summary>
        ///  Creates player stats from list of hands.
        /// </summary>
        public static List<PlayerStats> createFromHandList(List<Logic.Hand> handList, TableType tableType)
        {
            var statsList = new List<PlayerStats>();

            var playerNames =
                (from h in handList
                 from p in h.PlayersNames
                 select p).Distinct().ToArray();

            var client = handList.First().Client;
            Debug.Assert(handList.Select(item => item.Client).Distinct().Count() == 1);

            var dict = new Dictionary<string, HashSet<Hand>>();

            foreach (var playerName in playerNames)
            {
                dict.Add(playerName, new HashSet<Hand>());
            }

            foreach (var hand in handList)
            {
                foreach (var playerName in hand.PlayersNames)
                    dict[playerName].Add(hand);
            }

            var locker = new object();

            // Parallel update
            Parallel.ForEach(Partitioner.Create(0, playerNames.Length), range =>
            {
                for (var i = range.Item1; i < range.Item2; i++)
                {
                    var playerStats = new PlayerStats(playerNames[i], client, tableType);
                    var handsFromThisPlayer = dict[playerNames[i]];

                    foreach (var hand in handsFromThisPlayer)
                    {
                        playerStats.increment(hand);
                    }

                    lock (locker)
                    {
                        statsList.Add(playerStats);
                    }
                }
            });

            // Non parallel update
            /*foreach (var playerName in playerNames)
            {
                var playerStats = new PlayerStats(playerName, client, tableType);

                for (int i = 0; i < handList.Count; i++)
                {
                    playerStats.increment(handList[i]);
                }

                statsList.Add(playerStats);
            }*/

            return statsList;
        }
    }
}
