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
        public float brProb;
        public float ccProb;

        public ActionDistribution(float br, float cc)
        {
            brProb = br;
            ccProb = cc;

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

            if (brProb + ccProb > 1)
            {
                Console.WriteLine($"Warning: brProb + ccProb > 1; ccProb {ccProb}; brProb {brProb}");
                float sum = brProb + ccProb;

                ccProb = ccProb / sum;
                brProb = brProb / sum;
            }
        }

        public ActionType sample(Random prng)
        {
            var x = (float)prng.NextDouble();

            if (x < brProb)
            {
                return ActionType.Raise;
            }
            else if (x < brProb + ccProb)
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

                if (splitted_line.Count != 26)
                {
                    var err = $"Error: Not correct length of line in PreFlopChart {path}. Expected 26.";
                    Console.WriteLine(err);
                    throw new Exception(err);
                }

                for (var col=0; col < 13; col++)
                {
                    string brText = splitted_line[col*2];
                    float brProb = 0;
                    
                    if (float.TryParse(brText, out brProb))
                    {
                        brProb /= 100;
                    }
                    else
                    {
                        Console.WriteLine($"Warning: PreFlopChart {path} String '{brText}' could not be parsed.");
                    }

                    string ccText = splitted_line[col*2 + 1];
                    float ccProb = 0;

                    if (float.TryParse(ccText, out ccProb))
                    {
                        ccProb /= 100;
                    }
                    else
                    {
                        Console.WriteLine($"Warning: PreFlopChart {path} String '{ccText}' could not be parsed.");
                    }

                    string text = "";

                    if (row > col)
                        text = $"{rank_strings[col]}{rank_strings[row]}o";
                    else if (row < col)
                        text = $"{rank_strings[row]}{rank_strings[col]}s";
                    else
                        text = $"{rank_strings[row]}{rank_strings[col]}";

                    actionDist[text] = new ActionDistribution(brProb, ccProb);
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
