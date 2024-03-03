using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using G5.Logic;
using Action = G5.Logic.Action;
using System.Globalization;


namespace G5.Logic
{
    public static class ImportHH
    {
        ///////////////////////////////////////////////////////////////////////////////////////////
        // PokerStars
        ///////////////////////////////////////////////////////////////////////////////////////////

        // PokerStars Hand #149478314444:  Hold'em No Limit (€0.25/€0.50 EUR) - 2016/02/26 23:09:55 ET
        private static Int64 parseHandNumber_PokerStars(string line)
        {
            int first = line.IndexOf('#');
            int last = line.IndexOf(':');

            var numStr = line.Substring(first + 1, last - first - 1);
            return Int64.Parse(numStr);
        }

        private static int parseChips_PokerStars(string chipsStr)
        {
            var str = chipsStr.Substring(1);
            var flt = float.Parse(str, CultureInfo.InvariantCulture);
            return (int)(flt * 100 + 0.5f);
        }

        // Seat 4: Mmm Yum Yum (€251.96 in chips)
        private static bool parseSeat_PokerStars(Hand hand, string line)
        {
            if (!line.StartsWith("Seat "))
                return false;

            string playerName = "";
            int chips = 0;

            int startOfName = line.IndexOf(':');
            int openParen = line.LastIndexOf('(');
            int end = line.IndexOf(" in chips)");

            if (startOfName > -1 && openParen > -1 && end > -1)
            {
                playerName = line.Substring(startOfName + 2, openParen - startOfName - 3);

                var chipsStr = line.Substring(openParen + 1, end - openParen - 1);
                chips = parseChips_PokerStars(chipsStr);

                hand.addPlayer(playerName, chips);
                return true;
            }

            return false;
        }

        // AKconcrete: raises €1 to €1.50
        // Mmm Yum Yum: checks
        // AKconcrete: bets €5
        // Mmm Yum Yum: calls €5
        // AKconcrete: shows [5s As] (a flush, Ace high)
        // Mmm Yum Yum: mucks hand
        // AKconcrete: folds
        private static void parsePlayerAction_PokerStars(Hand hand, Street street, string line)
        {
            int first = line.LastIndexOf(":");

            var playerName = line.Substring(0, first);
            var actionStr = line.Substring(first + 2);
            var parts = actionStr.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (parts[0] == "raises")
            {
                int ammount = parseChips_PokerStars(parts[1]);
                hand.addAction(street, playerName, ActionType.Raise, ammount);
            }
            else if (parts[0] == "bets")
            {
                int ammount = parseChips_PokerStars(parts[1]);
                hand.addAction(street, playerName, ActionType.Bet, ammount);
            }
            else if (parts[0] == "checks")
            {
                hand.addAction(street, playerName, ActionType.Check, 0);
            }
            else if (parts[0] == "calls")
            {
                int ammount = parseChips_PokerStars(parts[1]);
                hand.addAction(street, playerName, ActionType.Call, ammount);
            }
            else if (parts[0] == "folds")
            {
                hand.addAction(street, playerName, ActionType.Fold, 0);
            }
        }

        /// <summary>
        /// Put small blind to be first in players list.
        /// </summary>
        private static void alignSmallBlind(Hand hand, string smallBlindName)
        {
            int sbInd = hand.PlayersNames.FindIndex((name) => name == smallBlindName);
            int numPlayers = hand.PlayersNames.Count;

            List<string> rotatedPlayers = new List<string>();
            List<int> rotatedStacks = new List<int>();

            for (int i = 0; i < numPlayers; i++)
            {
                int srcInd = (sbInd + i) % numPlayers;

                rotatedPlayers.Add(hand.PlayersNames[srcInd]);
                rotatedStacks.Add(hand.PlayerStacks[srcInd]);
            }

            hand.PlayersNames = rotatedPlayers;
            hand.PlayerStacks = rotatedStacks;
        }

        /// <summary>
        /// Loads hands from PokerStars hand history (files) and returns the list of hands.
        /// </summary>
        /// <returns></returns>
        public static List<Hand> loadHH_PokerStars(IEnumerable<string> fileNames, out int totalInvalidHands, PokerClient client)
        {
            totalInvalidHands = 0;
            var hands = new List<Hand>();

            foreach (var fileName in fileNames)
            {
                var allLines = File.ReadAllLines(fileName).ToList();
                Hand hand = null;
                Street street = Street.PreFlop;

                foreach (var line in allLines)
                {
                    // PokerStars Hand #149478314444:  Hold'em No Limit (€0.25/€0.50 EUR) - 2016/02/26 23:09:55 ET
                    if (line.StartsWith("PokerStars Hand #"))
                    {
                        if (hand != null)
                            hands.Add(hand);

                        hand = new Hand();

                        hand.HandNumber = parseHandNumber_PokerStars(line);
                        hand.Client = client;
                        hand.GameType = GameType.HoldEm;
                        // PlayerBalanceChanges = playerBalanceChanges,

                        street = Street.PreFlop;
                        continue;
                    }

                    // Table 'Apisaon' 6-max Seat #5 is the button
                    if (line.StartsWith("Table "))
                    {
                        continue;
                    }

                    // Seat 4: Mmm Yum Yum(€251.96 in chips)
                    if (parseSeat_PokerStars(hand, line))
                    {
                        continue;
                    }

                    // AKconcrete: posts small blind €0.25
                    if (line.Contains("posts small blind"))
                    {
                        int first = line.LastIndexOf(":");
                        string sbName = line.Substring(0, first);

                        // Small blind must be at place 0...
                        alignSmallBlind(hand, sbName);
                        continue;
                    }

                    // Mmm Yum Yum: posts big blind €0.50
                    // Mmm Yum Yum: posts big blind €0.50 and is all in
                    if (line.Contains("posts big blind"))
                    {
                        int first = line.LastIndexOf(":");
                        string name = line.Substring(0, first);
                        string chipsStr = line.Substring(first + 18);

                        int emptyPos = chipsStr.IndexOf(" ");

                        if (emptyPos > -1)
                        {
                            chipsStr = chipsStr.Substring(0, emptyPos);
                        }

                        hand.BigBlindSize = parseChips_PokerStars(chipsStr);
                        continue;
                    }

                    if (line == "*** HOLE CARDS ***")
                    {
                        street = Street.PreFlop;
                        continue;
                    }

                    // *** FLOP *** [6s 7s 4s]
                    if (line.StartsWith("*** FLOP ***"))
                    {
                        hand.Board.AddCard(new Card(line.Substring(14, 2)));
                        hand.Board.AddCard(new Card(line.Substring(17, 2)));
                        hand.Board.AddCard(new Card(line.Substring(20, 2)));
                        street = Street.Flop;
                        continue;
                    }

                    // *** TURN *** [6s 7s 4s] [Jh]
                    if (line.StartsWith("*** TURN ***"))
                    {
                        hand.Board.AddCard(new Card(line.Substring(25, 2)));
                        street = Street.Turn;
                        continue;
                    }

                    // *** RIVER *** [6s 7s 4s Jh] [Kc]
                    if (line.StartsWith("*** RIVER ***"))
                    {
                        hand.Board.AddCard(new Card(line.Substring(29, 2)));
                        street = Street.River;
                        continue;
                    }

                    foreach (var playerName in hand.PlayersNames)
                    {
                        if (line.StartsWith(playerName + ": "))
                        {
                            parsePlayerAction_PokerStars(hand, street, line);
                            continue;
                        }
                    }
                }
            }

            return hands;
        }
    }
}
