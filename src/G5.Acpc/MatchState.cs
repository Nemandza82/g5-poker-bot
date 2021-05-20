using G5.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace G5.Acpc
{
    class MatchState
    {
        public struct ActionInfo
        {
            public Logic.ActionType Type;
            public int Amount;

            public ActionInfo(Logic.ActionType type, int amount)
            {
                Type = type;
                Amount = amount;
            }

            public override string ToString()
            {
                if (Type == ActionType.Fold)
                    return "Pl folded";

                if (Type == ActionType.Check || Type == ActionType.Call)
                    return "Pl called";

                return "Pl raized " + Amount.ToString();
            }
        }

        /// <summary>
        /// The field tells the client their position relative to the dealer button. A value of 0 indicates that,
        /// the client is the first player after the button (the small blind in ring games, or the big blind in reverse-blind heads-up games).
        /// </summary>
        private int heroPosition;

        /// <summary>
        /// Hand number.
        /// </summary>
        public int handNumber;

        /// <summary>
        /// Current street.
        /// </summary>
        public Street street;

        /// <summary>
        /// List of actions for each street.
        /// </summary>
        public Dictionary<Street, List<ActionInfo>> betting;

        /// <summary>
        /// Hand balance at the end of hand (rg -100).
        /// </summary>
        public int balance;

        /// <summary>
        /// Hole cards for each player.
        /// </summary>
        private List<HoleCards> PlayerHoleCards;

        /// <summary>
        /// Card on the board.
        /// </summary>
        public Board board = new Board();

        public HoleCards heroHoleCards()
        {
            return PlayerHoleCards[heroPosition];
        }

        public HoleCards playerHoleCards(Position position, int numPlayers)
        {
            int pos = (int)position;

            if (numPlayers == 2)
            {
                // Small blind is button in HU
                if (position == Position.Button || position == Position.SmallBlind)
                    pos = 1;
                else
                    pos = 0;
            }

            return PlayerHoleCards[pos];
        }

        /// <summary>
        /// Returns true if here is in button position.
        /// </summary>
        public int acpcHeroPosition()
        {
            return heroPosition;
        }

        private class Parser
        {
            private string data;
            private int ind;

            public Parser(string str)
            {
                data = str;
                ind = 0;
            }

            public bool hasMore()
            {
                return ind < data.Length;
            }

            public string readUntil(string sep, bool consumeSep = true)
            {
                StringBuilder sb = new StringBuilder();

                while (ind < data.Length && !sep.Contains(data[ind]))
                    sb.Append(data[ind++]);

                if (consumeSep)
                    ind++;

                return sb.ToString();
            }

            public char readChar()
            {
                ind += 1;
                return data[ind - 1];
            }
        };

        /// <summary>
        /// Parses betting actions in one street. Example: r300r900c (There is raise for 300, re-raise 900 and a call).
        /// </summary>
        private static List<ActionInfo> parseBettingPerStreet(string bettingPerStreetStr, Street street, int heroPos)
        {
            var listOfActions = new List<ActionInfo>();
            Parser parser = new Parser(bettingPerStreetStr);

            while (parser.hasMore())
            {
                char actionChar = parser.readChar();
                ActionType type = ActionType.Fold;
                int ammount = 0;

                if (actionChar == 'c')
                {
                    type = ActionType.Call;
                }
                else if (actionChar == 'r')
                {
                    type = ActionType.Raise;
                    var ammStr = parser.readUntil("crf", false);
                    ammount = int.Parse(ammStr);
                }
                else // if (actionChar == 'f')
                {
                    type = ActionType.Fold;
                }

                listOfActions.Add(new ActionInfo(type, ammount));
            }

            return listOfActions;
        }

        /// <summary>
        /// Parses betting for all streets. Streets are separated by /. Example input: cc/r250c/r500c/r1250c.
        /// </summary>
        private static Street parseBetting(Dictionary<Street, List<ActionInfo>> actions, string bettingStr, int heroPos)
        {
            var parser = new Parser(bettingStr);
            Street street = Street.PreFlop;

            while (parser.hasMore() && street <= Street.River)
            {
                var perStreetStr = parser.readUntil("/");
                actions[street] = parseBettingPerStreet(perStreetStr, street, heroPos);
                street++;
            }

            return street;
        }

        /// <summary>
        /// Parses the whole cards of all players. Examples: |JdTc (Player 0 did not show hole cards, and player one has JdTc).
        /// Ad6h|| (Player 0 has Ad6h, and we do not know the cards of players 1 and 2).
        /// Ad6h||Td2h (Player 0 has Ad6h, player 2 has Td2h, and player 1 did not show cards).
        /// </summary>
        private static List<HoleCards> parseHoleCards(string holeCardsStr, int numPlayers)
        {
            var parser = new Parser(holeCardsStr);
            List<HoleCards> PlayerHoleCards = new List<HoleCards>();

            for (int i = 0; i < numPlayers; i++)
            {
                var pStr = parser.readUntil("|");
                PlayerHoleCards.Add((pStr != "") ? new HoleCards(pStr) : null);
            }

            return PlayerHoleCards;
        }

        /// <summary>
        /// Parses board cards (flop, turn, river). Example input: /TsKd7h/Kh/6d.
        /// </summary>
        private static void parseBoardCards(Board board, string boardStr)
        {
            var parser = new Parser(boardStr);

            while (parser.hasMore())
            {
                board.AddCard(new Card("" + parser.readChar() + parser.readChar()));
            }
        }

        /// <summary>
        /// Parses all cards, in player hands and on table. Example input: Ad6h||/TsKd7h/Kh/6d.
        /// </summary>
        private static Street parseCards(MatchState matchState, string cardStr, int numPlayers)
        {
            var parser = new Parser(cardStr);
            var street = Street.PreFlop;

            string holeCardsStr = parser.readUntil("/");
            matchState.PlayerHoleCards = parseHoleCards(holeCardsStr, numPlayers);

            while (parser.hasMore())
            {
                parseBoardCards(matchState.board, parser.readUntil("/"));
                street++;
            }

            return street;
        }

        /// <summary>
        /// Parses Acpc matchstate from string.
        /// String is in the form:
        /// MATCHSTATE:HeroPosition:HandNumber:BettingPreFlop/BettingFlop/BettingTurn/BettingRiver:CardsPl1|CardsPl2|CardsPl3/Flop/Turn/River
        /// Example hand:
        /// MATCHSTATE:0:90::Ad6h||
        /// S-> MATCHSTATE:0:90:c:Ad6h||
        /// S-> MATCHSTATE:0:90:cr:Ad6h||
        /// S-> MATCHSTATE:0:90:crf:Ad6h||
        /// S-> MATCHSTATE:0:90:crfc/:Ad6h||/TsKd7h
        /// S-> MATCHSTATE:0:90:crfc/r:Ad6h||/TsKd7h
        /// S-> MATCHSTATE:0:90:crfc/rc/:Ad6h||/TsKd7h/Kh
        /// S-> MATCHSTATE:0:90:crfc/rc/r:Ad6h||/TsKd7h/Kh
        /// S-> MATCHSTATE:0:90:crfc/rc/rc/:Ad6h||/TsKd7h/Kh/6d
        /// S-> MATCHSTATE:0:90:crfc/rc/rc/r:Ad6h||/TsKd7h/Kh/6d
        /// S-> MATCHSTATE:0:90:crfc/rc/rc/rc:Ad6h||Td2h/TsKd7h/Kh/6d
        /// </summary>
        public static MatchState Parse(string matchStateStr, int numPlayers)
        {
            try
            {
                var parser = new Parser(matchStateStr);
                string startStr = parser.readUntil(":");

                if (startStr != "MATCHSTATE")
                {
                    Console.WriteLine("Expected MATCHSTATE: at beggining of the message.");
                    return null;
                }

                var matchState = new MatchState();

                matchState.heroPosition = int.Parse(parser.readUntil(":"));
                matchState.handNumber = int.Parse(parser.readUntil(":"));

                matchState.betting = new Dictionary<Street, List<ActionInfo>>();
                var street = parseBetting(matchState.betting, parser.readUntil(":"), matchState.heroPosition);
                matchState.street = parseCards(matchState, parser.readUntil(""), numPlayers);

                return matchState;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Parses Acpc log line from string.
        /// String is in the form:
        /// STATE:759:r200c/cr400c/cr800f:3s4s|Jc8d/7d3c8c/Kh:-400|400:Intermission_2pn_2017|Feste_2pn_2017
        /// STATE:946:r273c/cc/r547c/cc:Jc7c|JhQd/9hAc5c/Js/Kh:-547|547:Feste_2pn_2017|Intermission_2pn_2017
        /// STATE:947:r300f:6s9d|7hAh:-100|100:Intermission_2pn_2017|Feste_2pn_2017
        /// STATE:949:cr550c/cc/cc/cc:5c5h|4hAd/7cQd3c/6h/9c:550|-550:Intermission_2pn_2017|Feste_2pn_2017
        /// STATE:950:cr300c/r600c/r1800f:KdAs|Ts9h/Ks6h4s/Tc:600|-600:Feste_2pn_2017|Intermission_2pn_2017
        /// STATE:951:r200c/cc/r616f:Jd2d|Ah4h/3d6hTd/8d:200|-200:Intermission_2pn_2017|Feste_2pn_2017
        /// STATE:952:r330c/cr957f:9c8s|KhKc/Ks9d6d:-330|330:Feste_2pn_2017|Intermission_2pn_2017
        /// </summary>
        public static MatchState ParseLog(string matchStateStr, int numPlayers)
        {
            try
            {
                var parser = new Parser(matchStateStr);
                string startStr = parser.readUntil(":");

                if (startStr != "STATE")
                {
                    Console.WriteLine("Expected MATCHSTATE: at beggining of the message.");
                    return null;
                }

                var matchState = new MatchState();

                // No hero position since no specific player is hero...
                matchState.heroPosition = 0;
                matchState.handNumber = int.Parse(parser.readUntil(":"));

                matchState.betting = new Dictionary<Street, List<ActionInfo>>();
                var street = parseBetting(matchState.betting, parser.readUntil(":"), matchState.heroPosition);
                matchState.street = parseCards(matchState, parser.readUntil(":"), numPlayers);

                Int32.TryParse(parser.readUntil("|"), out matchState.balance);

                return matchState;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }
    }
}
