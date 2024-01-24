using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace G5.Logic
{
    internal class PreFlopCharts
    {
        private Dictionary<Position, PreFlopChart> vs_0_bets_charts = new Dictionary<Position, PreFlopChart>();

        private Dictionary<Position, Dictionary<Position, PreFlopChart>> vs_1_bet_charts = 
            new Dictionary<Position, Dictionary<Position, PreFlopChart>>();

        private Dictionary<Position, Dictionary<Position, PreFlopChart>> vs_2_bets_reraise_charts =
            new Dictionary<Position, Dictionary<Position, PreFlopChart>>();

        private Dictionary<Position, Dictionary<Position, Dictionary<Position, PreFlopChart>>> vs_2_bets_charts =
            new Dictionary<Position, Dictionary<Position, Dictionary<Position, PreFlopChart>>>();

        private Dictionary<Position, Dictionary<Position, PreFlopChart>> vs_3_bets_charts =
            new Dictionary<Position, Dictionary<Position, PreFlopChart>>();

        private Dictionary<Position, Dictionary<Position, PreFlopChart>> vs_4_bets_charts =
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

            Console.WriteLine($"Warning shorthandToPosition is Empty");

            return Position.Empty;
        }

        public PreFlopCharts(string path)
        {
            Console.WriteLine($"Reading pre flop charts from {path}.");
            int numLoaded = 0;

            foreach (Position heroPosition in Enum.GetValues(typeof(Position)))
            {
                if (heroPosition != Position.Empty)
                {
                    vs_1_bet_charts[heroPosition] = new Dictionary<Position, PreFlopChart>();
                    vs_2_bets_reraise_charts[heroPosition] = new Dictionary<Position, PreFlopChart>();
                    vs_2_bets_charts[heroPosition] = new Dictionary<Position, Dictionary<Position, PreFlopChart>>();
                    vs_3_bets_charts[heroPosition] = new Dictionary<Position, PreFlopChart>();
                    vs_4_bets_charts[heroPosition] = new Dictionary<Position, PreFlopChart>();

                    foreach (Position villian1 in Enum.GetValues(typeof(Position)))
                    {
                        if (villian1 != Position.Empty)
                            vs_2_bets_charts[heroPosition][villian1] = new Dictionary<Position, PreFlopChart>();
                    }
                }
            }

            foreach (var file in Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                string[] parts = fileName.Split(new char[] { '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
                

                if (fileName.StartsWith("VS_0_Bets_") && parts.Length > 4)
                {
                    var position = shorthandToPosition(parts[4]);

                    if (position != Position.Empty)
                    {
                        vs_0_bets_charts[position] = new PreFlopChart(file);
                        Console.WriteLine($"Loaded pre flop chart for {position} RFI from {fileName}.");
                        numLoaded++;
                    }
                }
                else if (fileName.StartsWith("VS_1_Bet_") && parts.Length > 6)
                {
                    var position_hero = shorthandToPosition(parts[4]);
                    var position_villian = shorthandToPosition(parts[6]);

                    vs_1_bet_charts[position_hero][position_villian] = new PreFlopChart(file);
                    Console.WriteLine($"Loaded pre flop chart for {position_hero} facing RFI of {position_villian} from {fileName}.");
                    numLoaded++;
                }
                else if (fileName.StartsWith("VS_2_Bets_RR_") && parts.Length > 7)
                {
                    var position_hero = shorthandToPosition(parts[5]);
                    var position_villian = shorthandToPosition(parts[7]);

                    vs_2_bets_reraise_charts[position_hero][position_villian] = new PreFlopChart(file);
                    Console.WriteLine($"Loaded pre flop chart for {position_hero} facing reraise of {position_villian} from {fileName}.");
                    numLoaded++;
                }
                else if (fileName.StartsWith("VS_2_Bets_") && parts.Length > 7)
                {
                    var position_hero = shorthandToPosition(parts[4]);
                    var position_villian1 = shorthandToPosition(parts[6]);
                    var position_villian2 = shorthandToPosition(parts[7]);

                    vs_2_bets_charts[position_hero][position_villian1][position_villian2] = new PreFlopChart(file);
                    Console.WriteLine($"Loaded pre flop chart for {position_hero} facing raises of {position_villian1} and {position_villian2} from {fileName}.");
                    numLoaded++;
                }
                else if (fileName.StartsWith("VS_3_Bets_") && parts.Length > 6)
                {
                    var position_hero = shorthandToPosition(parts[4]);
                    var position_villian = shorthandToPosition(parts[6]);

                    vs_3_bets_charts[position_hero][position_villian] = new PreFlopChart(file);
                    Console.WriteLine($"Loaded pre flop chart for {position_hero} facing 3 bets of {position_villian} from {fileName}.");
                    numLoaded++;
                }
                else if (fileName.StartsWith("VS_4_Bets_") && parts.Length > 6)
                {
                    var position_hero = shorthandToPosition(parts[4]);
                    var position_villian = shorthandToPosition(parts[6]);

                    vs_4_bets_charts[position_hero][position_villian] = new PreFlopChart(file);
                    Console.WriteLine($"Loaded pre flop chart for {position_hero} facing 4 bets of {position_villian} from {fileName}.");
                    numLoaded++;
                }
            }

            Console.WriteLine($"Loaded {numLoaded} pre flop charts");
        }

        public ActionDistribution GetActionDistribution(BotGameState gameState)
        {
            if (gameState.getStreet() != Street.PreFlop)
                return null;

            var heroPos = gameState.getHero().PreFlopPosition;
            var bettorPositions = gameState.getBettors();

            if (gameState.getNumBets() == 0 && gameState.getNumCallers() == 0) // RFI
            {
                if (vs_0_bets_charts.ContainsKey(heroPos))
                    return vs_0_bets_charts[heroPos].GetActionDistribution(gameState.getHeroHoleCards());
            }
            else if (gameState.getNumBets() == 1 && gameState.getNumCallers() <= 1) // Facing RFI
            {
                if (!vs_1_bet_charts.ContainsKey(heroPos))
                    return null;

                if (bettorPositions.Count == 0)
                    return null;

                if (vs_1_bet_charts[heroPos].ContainsKey(bettorPositions.Last()))
                    return vs_1_bet_charts[heroPos][bettorPositions.Last()].GetActionDistribution(gameState.getHeroHoleCards());
            }
            else if (gameState.getNumBets() == 2 && gameState.getNumCallers() == 0)
            {
                if (gameState.getHero().LastAction == ActionType.Bet || gameState.getHero().LastAction == ActionType.Raise) // Facing re-raise
                {
                    if (!vs_2_bets_reraise_charts.ContainsKey(heroPos))
                        return null;

                    if (bettorPositions.Count == 0)
                        return null;

                    if (vs_2_bets_reraise_charts[heroPos].ContainsKey(bettorPositions.Last()))
                        return vs_2_bets_reraise_charts[heroPos][bettorPositions.Last()].GetActionDistribution(gameState.getHeroHoleCards());
                }
                else // 4bet (facing 2 bets first time)
                {
                    if (!vs_2_bets_charts.ContainsKey(heroPos))
                        return null;

                    if (bettorPositions.Count != 2)
                        return null;

                    var villian0Pos = bettorPositions[0];
                    var villian1Pos = bettorPositions[1];

                    if (vs_2_bets_charts[heroPos].ContainsKey(villian0Pos))
                    {
                        if (vs_2_bets_charts[heroPos][villian0Pos].ContainsKey(villian1Pos))
                            return vs_2_bets_charts[heroPos][villian0Pos][villian1Pos].GetActionDistribution(gameState.getHeroHoleCards());
                    }
                }
            }
            else if (gameState.getNumBets() == 3 && gameState.getNumCallers() == 0)
            {
                if (!vs_3_bets_charts.ContainsKey(heroPos))
                    return null;

                if (bettorPositions.Count == 0)
                    return null;

                if (vs_3_bets_charts[heroPos].ContainsKey(bettorPositions.Last()))
                    return vs_3_bets_charts[heroPos][bettorPositions.Last()].GetActionDistribution(gameState.getHeroHoleCards());
            }
            else if (gameState.getNumBets() == 4 && gameState.getNumCallers() == 0)
            {
                if (!vs_4_bets_charts.ContainsKey(heroPos))
                    return null;

                if (bettorPositions.Count == 0)
                    return null;

                if (vs_4_bets_charts[heroPos].ContainsKey(bettorPositions.Last()))
                    return vs_4_bets_charts[heroPos][bettorPositions.Last()].GetActionDistribution(gameState.getHeroHoleCards());
            }

            return null;
        }
    }
}
