using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace G5.Logic
{
    internal class PreFlopCharts
    {
        private Dictionary<Position, PreFlopChart> rfi_charts = new Dictionary<Position, PreFlopChart>();

        private Dictionary<Position, Dictionary<Position, PreFlopChart>> facing_rfi_charts = 
            new Dictionary<Position, Dictionary<Position, PreFlopChart>>();

        private Position shorthandToPosition(string shorthand)
        {
            if (shorthand == "UTG")
                return Position.UTG;
            else if (shorthand == "HJ")
                return Position.HJ;
            else if (shorthand == "CO")
                return Position.CutOff;
            else if (shorthand == "BTN")
                return Position.Button;
            else if (shorthand == "SB")
                return Position.SmallBlind;
            else if (shorthand == "BB")
                return Position.BigBlind;

            return Position.Unknown;
        }

        public PreFlopCharts(string path)
        {
            Console.WriteLine($"Reading pre flop charts from {path}.");

            facing_rfi_charts[Position.UTG] = new Dictionary<Position, PreFlopChart>();
            facing_rfi_charts[Position.HJ] = new Dictionary<Position, PreFlopChart>();
            facing_rfi_charts[Position.CutOff] = new Dictionary<Position, PreFlopChart>();
            facing_rfi_charts[Position.Button] = new Dictionary<Position, PreFlopChart>();
            facing_rfi_charts[Position.SmallBlind] = new Dictionary<Position, PreFlopChart>();
            facing_rfi_charts[Position.BigBlind] = new Dictionary<Position, PreFlopChart>();

            foreach (var file in Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                string[] parts = fileName.Split(new char[] { '_', '.' }, StringSplitOptions.RemoveEmptyEntries);

                if (fileName.StartsWith("RFI") && parts.Length > 1)
                {
                    var position = shorthandToPosition(parts[1]);

                    if (position != Position.Unknown)
                    {
                        rfi_charts[position] = new PreFlopChart(file);
                        Console.WriteLine($"Loaded pre flop chart for {position} RFI from {fileName}.");
                    }
                }
                else if (fileName.StartsWith("Facing_RFI_") && parts.Length > 5)
                {
                    var position_hero = shorthandToPosition(parts[3]);
                    var position_villian = shorthandToPosition(parts[5]);

                    facing_rfi_charts[position_hero][position_villian] = new PreFlopChart(file);
                    Console.WriteLine($"Loaded pre flop chart for {position_hero} facing RFI of {position_villian} from {fileName}.");
                }
            }
        }

        public ActionDistribution GetActionDistribution(BotGameState gameState)
        {
            if (gameState.getStreet() != Street.PreFlop)
                return null;

            var heroPos = gameState.getHero().PreFlopPosition;

            if (gameState.getNumBets() == 0 && gameState.getNumCallers() == 0)
            {
                if (rfi_charts.ContainsKey(heroPos))
                    return rfi_charts[heroPos].GetActionDistribution(gameState.getHeroHoleCards());
            }
            else if (gameState.getNumBets() == 1 && gameState.getNumCallers() <= 1)
            {
                if (!facing_rfi_charts.ContainsKey(heroPos))
                    return null;

                // At this point num bets must be 1
                // Find villian position
                Position villianPos = Position.Unknown;

                for (int i = 0; i < gameState.getPlayers().Count; i++)
                {
                    if (i == gameState.getHeroInd())
                        continue;

                    var villian = gameState.getPlayers()[i];

                    if (villian.LastAction == ActionType.Bet || villian.LastAction == ActionType.Raise)
                        villianPos = villian.PreFlopPosition;
                }

                if (villianPos == Position.Unknown)
                    return null;

                if (facing_rfi_charts[heroPos].ContainsKey(villianPos))
                    return facing_rfi_charts[heroPos][villianPos].GetActionDistribution(gameState.getHeroHoleCards());
            }

            return null;
        }
    }
}
