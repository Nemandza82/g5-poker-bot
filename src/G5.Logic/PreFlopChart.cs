using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace G5.Logic
{
    internal class ActionDistribution
    {
        public float allinProb;
        public float brProb;
        public float ccProb;

        public ActionDistribution(float allin, float br, float cc)
        {
            allinProb = allin;
            brProb = br;
            ccProb = cc;

            if (allinProb < 0)
            {
                Console.WriteLine($"Warning: allinProb < 0, {allinProb}");
                brProb = 0;
            }

            if (brProb < 0)
            {
                Console.WriteLine($"Warning: brProb < 0, {brProb}");
                brProb = 0;
            }

            if (ccProb < 0)
            {
                Console.WriteLine($"Warning: ccProb < 0, {ccProb}");
                ccProb = 0;
            }

            if (allinProb + brProb + ccProb > 1)
            {
                Console.WriteLine($"Warning: allinProb + brProb + ccProb > 1; allinProb {allinProb}; brProb {brProb}; ccProb {ccProb}");
                float sum = allinProb + brProb + ccProb;

                allinProb = allinProb / sum;
                ccProb = ccProb / sum;
                brProb = brProb / sum;
            }
        }

        public ActionType sample(Random prng)
        {
            var x = (float)prng.NextDouble();

            if (x < allinProb)
            {
                return ActionType.AllIn;
            }
            else if (x < allinProb + brProb)
            {
                return ActionType.Raise;
            }
            else if (x < allinProb + brProb + ccProb)
            {
                return ActionType.Call;
            }
            else
            {
                return ActionType.Fold;
            }
        }
    };


    internal class PreFlopChart
    {
        private Dictionary<string, ActionDistribution> actionDist = new Dictionary<string, ActionDistribution>();

        public PreFlopChart(string path)
        {
            string[] rank_strings = { "A", "K", "Q", "J", "T", "9", "8", "7", "6", "5", "4", "3", "2" };

            var lines = File.ReadAllLines(path).ToList();

            if (lines.Count() > 0)
                lines.RemoveAt(0);

            if (lines.Count() != 13)
            {
                var err = $"Error: Not correct number of lines in PreFlopChart {path}. Expected 13.";
                Console.WriteLine(err);
                throw new Exception(err);
            }

            for (var row=0; row < lines.Count; row++)
            {
                var splitted_line = lines[row].Split(new[] { ',', ' ', '\t', '\n' }).ToList();
                splitted_line.RemoveAll(x => x == "");

                if (splitted_line.Count > 0)
                    splitted_line.RemoveAt(0);

                if (splitted_line.Count != 39)
                {
                    var err = $"Error: Not correct length of line in PreFlopChart {path}. Expected 39.";
                    Console.WriteLine(err);
                    throw new Exception(err);
                }

                for (var col=0; col < 13; col++)
                {
                    string allinText = splitted_line[col * 3];
                    float allinProb = 0;

                    if (float.TryParse(allinText, out allinProb))
                    {
                        allinProb /= 100;
                    }
                    else
                    {
                        Console.WriteLine($"Warning: PreFlopChart {path}, row {row}, col {col} allinText '{allinText}' could not be parsed.");
                    }

                    string brText = splitted_line[col*3 + 1];
                    float brProb = 0;
                    
                    if (float.TryParse(brText, out brProb))
                    {
                        brProb /= 100;
                    }
                    else
                    {
                        Console.WriteLine($"Warning: PreFlopChart {path}, row {row}, col {col} brText '{brText}' could not be parsed.");
                    }

                    string ccText = splitted_line[col*3 + 2];
                    float ccProb = 0;

                    if (float.TryParse(ccText, out ccProb))
                    {
                        ccProb /= 100;
                    }
                    else
                    {
                        Console.WriteLine($"Warning: PreFlopChart {path}, row {row}, col {col} ccText '{ccText}' could not be parsed.");
                    }

                    string text = "";

                    if (row > col)
                        text = $"{rank_strings[col]}{rank_strings[row]}o";
                    else if (row < col)
                        text = $"{rank_strings[row]}{rank_strings[col]}s";
                    else
                        text = $"{rank_strings[row]}{rank_strings[col]}";

                    actionDist[text] = new ActionDistribution(allinProb, brProb, ccProb);
                }
            }
        }

        public ActionDistribution GetActionDistribution(HoleCards holeCards)
        {
            string suite = (holeCards.Card0.suite == holeCards.Card1.suite) ? "s" : "o";

            if (holeCards.Card0.rank == holeCards.Card1.rank)
                suite = "";

            string rank_str = "";

            if (holeCards.Card0.rank >= holeCards.Card1.rank)
            {
                rank_str += Card.RankToString(holeCards.Card0.rank);
                rank_str += Card.RankToString(holeCards.Card1.rank);
            }
            else
            {
                rank_str += Card.RankToString(holeCards.Card1.rank);
                rank_str += Card.RankToString(holeCards.Card0.rank);
            }

            string key = rank_str + suite;
            return actionDist[key];
        }
    }
}
