using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace G5.Logic
{
    /// <summary>
    /// Ruka za HandHistory pamti ID, ActionList, Board, HoleCards, Player names...
    /// </summary>
    public class Hand
    {
        public Int64 HandNumber { get; set; }
        public PokerClient Client { get; set; }

        public GameType GameType { get; set; }
        public int BigBlindSize { get; set; }

        public string HeroName { get; set; }

        /// <summary>
        /// Small Blind, BigBlind, Midle1, Midle2, Cutoff, Button for 6 max...
        /// </summary>
        public List<string> PlayersNames;

        public HoleCards HeroHoleCards;
        public Board Board;
        public List<Action> ActionList;
        public List<int> PlayerBalanceChanges;
        public List<int> PlayerStacks;

        public Hand()
        {
            Client = PokerClient.G5;
            BigBlindSize = 4;
            HeroName = "";

            PlayersNames = new List<string>();
            HeroHoleCards = null;
            ActionList = new List<Action>();
            Board = new Board();
            PlayerBalanceChanges = new List<int>();
            PlayerStacks = new List<int>();
        }

        public override bool Equals(object other)
        {
            if (other == null)
                return false;

            if (GetType() != other.GetType())
                return false;

            if (this == other)
                return true;

            var otherHand = (Hand)other;
            return otherHand.HandNumber == HandNumber;
        }

        public override int GetHashCode()
        {
            return HandNumber.GetHashCode();
        }

        public override string ToString()
        {
            string s = Client + " - " +
                       HeroName + " - " +
                       GameType + " - <" +
                       BigBlindSize + "> - " +
                       HandNumber + " - " + "\r\n\r\n";

            for (int i = 0; i < PlayersNames.Count; i++)
            {
                s += PlayersNames[i];

                if (i < PlayerStacks.Count)
                    s += " <" + PlayerStacks[i] + "> ";

                if (i < PlayerBalanceChanges.Count)
                    s += " <" + PlayerBalanceChanges[i] + ">";

                s += "\r\n";
            }

            s += "\r\nHole Cards: ";
            s += string.Join(" ", HeroHoleCards);

            s += "\r\n";
            s += "Board: ";
            s += Board.ToString();

            s += "\r\n\r\n";

            s += string.Join("\r\n", ActionList);

            s += "\r\n";

            return s;
        }

        public void setHoleCardsHoldem(Card card0, Card card1)
        {
            HeroHoleCards = new HoleCards(card0, card1);
        }

        public void addPlayer(string playerName, int playerStack)
        {
            if (!PlayersNames.Contains(playerName))
            {
                PlayersNames.Add(playerName);
                PlayerStacks.Add(playerStack);
            }
        }

        public void addPlayerWinnings(Dictionary<string, int> playerWinnings)
        {
            var moneyBalance = 0;

            foreach (string playerName in PlayersNames)
            {
                int moneyWon;
                playerWinnings.TryGetValue(playerName, out moneyWon);

                moneyBalance += moneyWon;
                PlayerBalanceChanges.Add(moneyWon);
            }

            Debug.Assert(Math.Abs(moneyBalance) < 11);
        }

        public void addAction(Street street, string playerName, ActionType type, int ammount)
        {
            ActionList.Add(new Action(street, playerName, type, ammount));
        }
    }
}
