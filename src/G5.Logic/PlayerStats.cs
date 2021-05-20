using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;


namespace G5.Logic
{
    public class PlayerStats
    {
        public string PlayerName { get; private set; }
        public PokerClient Client { get; private set; }
        private TableType _tableType;
        public StatValue VPIP { get; private set; }

        private List<PreFlopParams> _allPreFlopParams;
        private List<PostFlopParams> _allPostFlopParams;

        public ActionStats[] PreFlopStats { get; private set; }
        public ActionStats[] PostFlopStats { get; private set; }

        public PlayerStats(string playerName, PokerClient client, TableType tableType)
        {
            PlayerName = playerName;
            Client = client;
            _tableType = tableType;
            VPIP = new StatValue();

            _allPreFlopParams = PreFlopParams.getAllParams(_tableType);
            _allPostFlopParams = PostFlopParams.getAllParams(_tableType);

            PreFlopStats = new ActionStats[_allPreFlopParams.Count];
            PostFlopStats = new ActionStats[_allPostFlopParams.Count];

            for (int i = 0; i < PreFlopStats.Length; i++)
                PreFlopStats[i] = new ActionStats();

            for (int i = 0; i < PostFlopStats.Length; i++)
                PostFlopStats[i] = new ActionStats();
        }

        public PlayerStats(string playerName, PokerClient client, TableType tableType, int vpipPositive, int vpipTotal,
            ActionStats[] preFlopAD, ActionStats[] postFlopAD)
        {
            PlayerName = playerName;
            Client = client;
            _tableType = tableType;
            VPIP = new StatValue(vpipPositive, vpipTotal);

            _allPreFlopParams = PreFlopParams.getAllParams(tableType);
            _allPostFlopParams = PostFlopParams.getAllParams(tableType);

            PreFlopStats = preFlopAD;
            PostFlopStats = postFlopAD;
        }

        public int serialize(BinaryWriter writer)
        {
            writer.Write(PlayerName);
            writer.Write(Client.ToString());
            writer.Write((int)_tableType);
            writer.Write(VPIP.PositiveSamples);
            writer.Write(VPIP.TotalSamples);

            writer.Write(PreFlopStats.Length);
            int total = 0;

            foreach (var ad in PreFlopStats)
            {
                ad.serialize(writer);
                total += ad.totalSamples();
            }

            writer.Write(PostFlopStats.Length);

            foreach (var ad in PostFlopStats)
                ad.serialize(writer);

            return total;
        }

        private static PokerClient strToPokerClient(string clientStr)
        {
            return (PokerClient)Enum.Parse(typeof(PokerClient), clientStr);
        }

        public static PlayerStats deserialize(BinaryReader reader)
        {
            var playerName = reader.ReadString();
            var client = strToPokerClient(reader.ReadString());
            var totalPlayers = reader.ReadInt32();
            var vpipPositive = reader.ReadInt32();
            var vpipTotal = reader.ReadInt32();

            var preFlopTotal = reader.ReadInt32();
            var preFlopAds = new ActionStats[preFlopTotal];

            for (int i = 0; i < preFlopTotal; i++)
                preFlopAds[i] = ActionStats.deserialize(reader);

            var postFlopTotal = reader.ReadInt32();
            var postFlopAds = new ActionStats[postFlopTotal];

            for (int i = 0; i < postFlopTotal; i++)
                postFlopAds[i] = ActionStats.deserialize(reader);

            return new PlayerStats(playerName, client, (TableType)totalPlayers, vpipPositive, vpipTotal, preFlopAds, postFlopAds);
        }

        public static List<PlayerStats> loadStatsList(string fileName)
        {
            if (File.Exists(fileName))
            {
                var startTime = DateTime.Now;

                using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
                {
                    int numStats = reader.ReadInt32();
                    var fullStatsList = new List<PlayerStats>();

                    for (int i = 0; i < numStats; i++)
                        fullStatsList.Add(PlayerStats.deserialize(reader));

                    var timeSpentSeconds = (DateTime.Now - startTime).TotalSeconds;
                    return fullStatsList;
                }
            }
            else
            {
                Console.WriteLine("File " + fileName + " not found.");
            }

            return null;
        }

        public int TotalHands
        {
            get
            {
                int total = 0;

                for (int i = 0; i < _allPreFlopParams.Count; i++)
                {
                    if (_allPreFlopParams[i].PreviousAction == ActionType.Fold)
                        total += PreFlopStats[i].totalSamples();
                }

                return total;
            }
        }

        public ActionStats getPreFlopStats(PreFlopParams preFlopParams)
        {
            Debug.Assert(preFlopParams.TableType == _tableType);
            return PreFlopStats[preFlopParams.ToIndex()];
        }

        public ActionStats getPostFlopStats(PostFlopParams postFlopParams)
        {
            Debug.Assert(postFlopParams.TableType == _tableType);
            return PostFlopStats[postFlopParams.ToIndex()];
        }

        public void calculatePFR(out int betRaiseCount, out int totalCount)
        {
            ActionStats totalAD = new ActionStats();

            for (int i = 0; i < _allPreFlopParams.Count; i++)
            {
                if (_allPreFlopParams[i].NumRaises == 0)
                    totalAD.append(PreFlopStats[i]);
            }

            betRaiseCount = totalAD.BetRaiseSamples;
            totalCount = totalAD.totalSamples();
        }

        public void calculateAggression(out int raiseCount, out int totalCount)
        {
            var totalForcedStats = new ActionStats();
            var totalUnforcedStats = new ActionStats();

            for (int i = 0; i < _allPostFlopParams.Count; i++)
            {
                if (_allPostFlopParams[i].NumBets > 0)
                    totalForcedStats.append(PostFlopStats[i]);

                if (_allPostFlopParams[i].NumBets == 0)
                    totalUnforcedStats.append(PostFlopStats[i]);
            }

            raiseCount = totalForcedStats.BetRaiseSamples + totalUnforcedStats.BetRaiseSamples; // bets + raises
            totalCount = raiseCount + totalForcedStats.CheckCallSamples;  // bets + raises + calls
        }

        public void calculateWTP(out int positiveCount, out int totalCount)
        {
            var totalForcedStats = new ActionStats();

            for (int i = 0; i < _allPostFlopParams.Count; i++)
            {
                if (_allPostFlopParams[i].NumBets > 0)
                    totalForcedStats.append(PostFlopStats[i]);
            }

            positiveCount = totalForcedStats.CheckCallSamples + totalForcedStats.BetRaiseSamples;
            totalCount = positiveCount + totalForcedStats.FoldSamples;
        }

        public static PlayerStats createStatsFromHandList(List<Hand> handList, string playerName, PokerClient client, TableType tableType)
        {
            var playerStats = new PlayerStats(playerName, client, tableType);

            foreach (var hand in handList)
                playerStats.increment(hand);

            return playerStats;
        }

        private static Position getPlayerPosition(List<string> playerList, string playerName)
        {
            int playerPos = playerList.FindIndex(name => name == playerName);

            if (playerPos == 0)
                return Position.SmallBlind;

            if (playerPos == 1)
                return Position.BigBlind;

            if (playerPos == playerList.Count - 1)
                return Position.Button;

            if (playerPos == playerList.Count - 2)
                return Position.CutOff;

            if (playerPos == playerList.Count - 3)
                return Position.Middle2;

            if (playerPos == playerList.Count - 4)
                return Position.Middle1;

            Debug.Assert(false);
            return Position.Button;
        }

        private static bool inPosition(List<string> playerList, string playerName, int numPlayers)
        {
            if (playerList.Count == 0)
                return false;

            if (numPlayers == 2)
                return playerList[0] == playerName;

            return playerList[playerList.Count - 1] == playerName;
        }

        public void increment(Hand hand)
        {
            Debug.Assert(hand.Client == Client);
            Debug.Assert(hand.PlayersNames.Count <= (int)_tableType);

            var allInList = new List<string>();
            var activePlayerList = new List<string>(hand.PlayersNames);
            int playerIndex = activePlayerList.FindIndex(name => name == PlayerName);

            if (playerIndex < 0)
                return;

            Position position = getPlayerPosition(activePlayerList, PlayerName);
            var lastPlayerAction = ActionType.Fold;
            bool playerPutMoneyInPot = false;
            bool playerFoldedOrAllIn = false;

            // Pre-Flop
            {
                int numRaises = 0;
                int numCallers = 0;

                foreach (Action a in hand.ActionList.Where(a => a.Street == Street.PreFlop && a.IsValidAction))
                {
                    if (playerFoldedOrAllIn || activePlayerList.Count <= 1)
                        break;

                    if (a.PlayerName == PlayerName)
                    {
                        int numPlayers = activePlayerList.Count + allInList.Count;

                        var preFlopParams = new PreFlopParams(_tableType,
                            position,
                            numCallers,
                            numRaises,
                            numPlayers,
                            lastPlayerAction,
                            inPosition(activePlayerList, a.PlayerName, numPlayers));

                        getPreFlopStats(preFlopParams).addSample(a.Type);

                        if (a.Type == ActionType.Fold || a.Type == ActionType.AllIn)
                        {
                            playerFoldedOrAllIn = true;
                        }

                        if (a.IsRaiseAction || a.Type == ActionType.Call)
                        {
                            playerPutMoneyInPot = true;
                        }

                        lastPlayerAction = a.Type;
                    }
                    else
                    {
                        if (a.Type == ActionType.Fold)
                        {
                            activePlayerList.Remove(a.PlayerName);
                        }
                        else if (a.Type == ActionType.AllIn)
                        {
                            activePlayerList.Remove(a.PlayerName);
                            allInList.Add(a.PlayerName);
                        }
                    }

                    if (a.IsRaiseAction)
                    {
                        numRaises++;
                        numCallers = 0;
                    }
                    else if (a.Type == ActionType.Call)
                    {
                        numCallers++;
                    }
                }
            }

            // VPIP
            VPIP.AddSample(playerPutMoneyInPot);

            // For each remaining streets
            for (var currentStreet = Street.Flop; currentStreet <= Street.River; currentStreet++)
            {
                if (playerFoldedOrAllIn || activePlayerList.Count <= 1)
                    break;

                int round = 0;
                int numRaises = 0;

                // For each action on current street
                foreach (Action a in hand.ActionList.Where(a => (a.Street == currentStreet) && a.IsValidAction))
                {
                    if (a.PlayerName == PlayerName)
                    {
                        var numPlayers = activePlayerList.Count + allInList.Count;

                        var postFlopParams = new PostFlopParams(_tableType,
                            currentStreet,
                            round,
                            lastPlayerAction,
                            numRaises,
                            inPosition(activePlayerList, a.PlayerName, numPlayers),
                            numPlayers);

                        //round == 1 && lastPlayerAction == ActionType.Check && postFlopParams.InPosition == true
                        getPostFlopStats(postFlopParams).addSample(a.Type);

                        if (a.Type == ActionType.Fold || a.Type == ActionType.AllIn)
                        {
                            playerFoldedOrAllIn = true;
                            break;
                        }

                        lastPlayerAction = a.Type;
                        round++;
                    }
                    else
                    {
                        if (a.Type == ActionType.Fold)
                        {
                            activePlayerList.Remove(a.PlayerName);
                        }
                        else if (a.Type == ActionType.AllIn)
                        {
                            activePlayerList.Remove(a.PlayerName);
                            allInList.Add(a.PlayerName);
                        }
                    }

                    if (a.IsRaiseAction)
                        numRaises++;
                }
            }
        }

        public override string ToString()
        {
            return PlayerName + " (" + Client + "), VPIP: " + VPIP;
        }
    }
}
