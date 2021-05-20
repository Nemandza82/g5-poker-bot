using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace G5.Logic
{
    public class Pot
    {
        public static List<int> calculateWinnings(List<Player> players, List<int> handStrengths)
        {
            int[] potHeight = new int[6];
            int[] moneyInPot = new int[6];
            int[] maxHandStrength = new int[6];
            int[] ties = new int[6];

            for (int i = 0; i < 6; i++)
            {
                potHeight[i] = 0;
                moneyInPot[i] = 0;
                maxHandStrength[i] = 0;
                ties[i] = 0;
            }

            int prevHeight = 0;
            bool newHeightExists = true;
            int nPots = 0;

            while (newHeightExists)
            {
                int curHeight = int.MaxValue;
                newHeightExists = false;

                foreach (Player player in players)
                {
                    if ((player.StatusInHand != Status.Folded) && (player.MoneyInPot > prevHeight))
                    {
                        curHeight = Math.Min(player.MoneyInPot, curHeight);
                        newHeightExists = true;
                    }
                }

                if (newHeightExists)
                {
                    potHeight[nPots] = curHeight;
                    moneyInPot[nPots] = 0;
                    maxHandStrength[nPots] = 0;

                    foreach (Player player in players)
                    {
                        if (player.MoneyInPot > prevHeight)
                            moneyInPot[nPots] += Math.Min(player.MoneyInPot, curHeight) - prevHeight;
                    }

                    prevHeight = curHeight;
                    nPots++;
                }
            }

            // Add hand strengts for each player
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].StatusInHand == Status.Folded)
                    continue;

                for (int j = 0; j < nPots; j++)
                {
                    if (players[i].MoneyInPot >= potHeight[j])
                    {
                        if (maxHandStrength[j] < handStrengths[i])
                        {
                            maxHandStrength[j] = handStrengths[i];
                            ties[j] = 1;
                        }
                        else if (maxHandStrength[j] == handStrengths[i])
                        {
                            ties[j]++;
                        }
                    }
                }
            }

            // Calculate winning for each player
            var winnings = new List<int>();

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].StatusInHand == Status.Folded)
                {
                    winnings.Add(0);
                    continue;
                }

                float winning = 0;

                for (int j = 0; (j < nPots) && (players[i].MoneyInPot >= potHeight[j]); j++)
                {
                    if (handStrengths[i] >= maxHandStrength[j])
                    {
                        Debug.Assert(ties[j] > 0);
                        winning += moneyInPot[j] / (float)ties[j];
                    }
                }

                winnings.Add((int)winning);
            }

            return winnings;
        }
    }
}
