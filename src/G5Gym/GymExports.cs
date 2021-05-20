using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace G5Gym
{
    public class GymExports
    {
        private GymGame _game;

        public GymExports()
        {
            _game = new GymGame(G5.Logic.TableType.HeadsUp, new bool[] { false, true });
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

        public dynamic startHand()
        {
            return _game.startNewHand();
        }

        public dynamic act(string action, int byAmmount, bool isFakeAction)
        {
            var actionType = G5.Logic.ActionType.Fold;

            if (action == "f")
            {
                actionType = G5.Logic.ActionType.Fold;
            }
            else if (action == "cc")
            {
                actionType = G5.Logic.ActionType.Check;
            }
            else if (action == "br" || action == "ai")
            {
                actionType = G5.Logic.ActionType.Bet;
            }

            return _game.playerActs(actionType, byAmmount, isFakeAction);
        }
    }
}
