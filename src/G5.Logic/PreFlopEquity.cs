using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;


namespace G5.Logic
{
    /// <summary>
    /// Pomocna klasa za racunanje pre-flop equitija za sve moguce parove Hole-Cards.
    /// </summary>
    public class PreFlopEquity
    {
        private const string fileName = @"PreFlopEquities.txt";
        private const int N_HOLECARDS_DOUBLE = 51 * 52 + 52;

        private static float[][] _data;

        /// <summary>
        /// Loads saved all pairs pre flop equities.
        /// </summary>
        public static void Load()
        {
            _data = new float[N_HOLECARDS_DOUBLE][];

            for (int i = 0; i < N_HOLECARDS_DOUBLE; i++)
            {
                _data[i] = new float[N_HOLECARDS_DOUBLE];

                for (int j = 0; j < N_HOLECARDS_DOUBLE; j++)
                    _data[i][j] = 0;
            }

            using (var file = new StreamReader(File.OpenRead(fileName)))
            {
                while (!file.EndOfStream)
                {
                    string line = file.ReadLine();

                    HoleCards hand1 = new HoleCards(line.Substring(0, 4));
                    HoleCards hand2 = new HoleCards(line.Substring(5, 4));

                    float eq = float.Parse(line.Substring(10), CultureInfo.InvariantCulture);
                    _data[hand1.ToInt()][hand2.ToInt()] = eq;
                }
            }
        }

        private static int TotalCombinations()
        {
            int total = 0;

            for (int i = 0; i < N_HOLECARDS_DOUBLE; i++)
            {
                for (int j = 0; j < N_HOLECARDS_DOUBLE; j++)
                {
                    HoleCards hand1 = new HoleCards(i);
                    HoleCards hand2 = new HoleCards(j);

                    // Hand1 not valid
                    if (hand1.Card0.ToInt() >= hand1.Card1.ToInt())
                        continue;

                    // Hand2 not valid
                    if (hand2.Card0.ToInt() >= hand2.Card1.ToInt())
                        continue;

                    // At least one card in each hand is the same => not valid
                    if (hand1.Card0.ToInt() == hand2.Card0.ToInt() ||
                        hand1.Card0.ToInt() == hand2.Card1.ToInt() ||
                        hand1.Card1.ToInt() == hand2.Card0.ToInt() ||
                        hand1.Card1.ToInt() == hand2.Card1.ToInt())
                        continue;

                    total++;
                }
            }

            return total;
        }

        public class ProgressReport
        {
            public int Total;
            public int Done;
            public float MsPerCombination;

            public ProgressReport(int t, int d, float s)
            {
                Total = t;
                Done = d;
                MsPerCombination = s;
            }
        }

        public interface IProgressReporter
        {
            void ReportProgress(int percent, object userState);
            bool isCancellationPending();
        }

        /// <summary>
        /// Calculates PFR for all hole cards against all other hole cards.
        /// Last couple hours. Saved to file afterwards.
        /// </summary>
        /// <param name="backgroundWorker">For reporting progress</param>
        public static void AllPairsPreFlopEquity(IProgressReporter progressReporter)
        {
            int total = TotalCombinations();
            int done = 0;

            for (int i = 0; i < N_HOLECARDS_DOUBLE; i++)
            {
                for (int j = 0; j < N_HOLECARDS_DOUBLE; j++)
                {
                    if (_data[i][j] > 0)
                        done++;
                }
            }

            if (progressReporter != null)
                progressReporter.ReportProgress((100 * done) / total, new ProgressReport(total, done, 0));

            for (int i = 0; i < 51; i++)
            {
                if (progressReporter != null && progressReporter.isCancellationPending())
                    break;

                for (int j = i + 1; j < 52; j++)
                {
                    if (progressReporter != null && progressReporter.isCancellationPending())
                        break;

                    // i < j
                    HoleCards heroHand = new HoleCards(i, j);

                    for (int k = 0; k < 51; k++)
                    {
                        if (progressReporter != null && progressReporter.isCancellationPending())
                            break;

                        if (k == i || k == j)
                            continue;

                        float[] tmpEquity = new float[52];

                        for (int ppp = 0; ppp < 52; ppp++)
                        {
                            tmpEquity[ppp] = 0;
                        }

                        DateTime startTime = DateTime.Now;
                        int previousDone = done;

                        //for (int l = k + 1; l < 52; l++ )
                        Parallel.For(k + 1, 52, (l) =>
                        {
                            if (l != i && l != j)
                            {
                                // k < l
                                HoleCards villanHand = new HoleCards(k, l);

                                if (_data[heroHand.ToInt()][villanHand.ToInt()] == 0)
                                {
                                    tmpEquity[l] = Calculate(heroHand, villanHand);
                                }
                            }
                        });

                        using (var file =  File.AppendText(fileName))
                        {
                            for (int l = k + 1; l < 52; l++)
                            {
                                if (tmpEquity[l] > 0)
                                {
                                    HoleCards villanHand = new HoleCards(k, l);
                                    done += AddToData(file, heroHand, villanHand, tmpEquity[l]);
                                }
                            }
                        }

                        DateTime endTime = DateTime.Now;
                        TimeSpan dTime = endTime - startTime;

                        float msPerComb = (done > previousDone) ? (float)(dTime.TotalMilliseconds / (done - previousDone)) : 0;

                        if (progressReporter != null)
                            progressReporter.ReportProgress((100 * done) / total, new ProgressReport(total, done, msPerComb));
                    }
                }
            }
        }

        private static int AddToData(System.IO.StreamWriter file, HoleCards hand1, HoleCards hand2, float equity)
        {
            int total = 0;

            for (Card.Suite s11 = Card.Suite.Clubs; s11 <= Card.Suite.Spades; s11++)
            {
                for (Card.Suite s12 = Card.Suite.Clubs; s12 <= Card.Suite.Spades; s12++)
                {
                    if (!DoSuitsAgree(hand1.Card1.suite, hand1.Card0.suite, s12, s11))
                        continue;

                    for (Card.Suite s21 = Card.Suite.Clubs; s21 <= Card.Suite.Spades; s21++)
                    {
                        if (!DoSuitsAgree(hand2.Card0.suite, hand1.Card0.suite, s21, s11))
                            continue;

                        if (!DoSuitsAgree(hand2.Card0.suite, hand1.Card1.suite, s21, s12))
                            continue;

                        for (Card.Suite s22 = Card.Suite.Clubs; s22 <= Card.Suite.Spades; s22++)
                        {
                            if (!DoSuitsAgree(hand2.Card1.suite, hand1.Card0.suite, s22, s11))
                                continue;

                            if (!DoSuitsAgree(hand2.Card1.suite, hand1.Card1.suite, s22, s12))
                                continue;

                            if (!DoSuitsAgree(hand2.Card1.suite, hand2.Card0.suite, s22, s21))
                                continue;

                            HoleCards hand1_new = new HoleCards(hand1.Card0.rank, s11, hand1.Card1.rank, s12);
                            HoleCards hand2_new = new HoleCards(hand2.Card0.rank, s21, hand2.Card1.rank, s22);

                            total += AddToDataTmp(file, hand1_new, hand2_new, equity);
                            total += AddToDataTmp(file, hand2_new, hand1_new, 1.0f - equity);
                        }
                    }
                }
            }

            return total;
        }

        private static bool DoSuitsAgree(Card.Suite s1_old, Card.Suite s2_old, Card.Suite s1_new, Card.Suite s2_new)
        {
            return (s1_old == s2_old && s1_new == s2_new) || (s1_old != s2_old && s1_new != s2_new);
        }

        private static int AddToDataTmp(System.IO.StreamWriter file, HoleCards hand1, HoleCards hand2, float equity)
        {
            int total = 0;

            if (!(_data[hand1.ToInt()][hand2.ToInt()] > 0))
            {
                _data[hand1.ToInt()][hand2.ToInt()] = equity;
                file.WriteLine(hand1.ToString() + ":" + hand2.ToString() + "=" + (equity * 100).ToString("f2"));
                total++;
            }

            return total;
        }

        /// <summary>
        /// Calculates Pre-Flop-Equity of particular hole cards against particular hole cards of the villian.
        /// </summary>
        public static float Calculate(HoleCards heroHoleCards, HoleCards villanHoleCards)
        {
            int[] aheadGlobal = new int[52];
            int[] tieGlobal = new int[52];
            int[] behindGlobal = new int[52];

            for (int i = 0; i < 52; i++)
            {
                aheadGlobal[i] = 0;
                tieGlobal[i] = 0;
                behindGlobal[i] = 0;
            }

            //for (int i0 = 0; i0 < 48; i0++)  // Flop 1
            Parallel.For(0, 48, (i0) =>
            {
                CalculateTmp(aheadGlobal, tieGlobal, behindGlobal, heroHoleCards, villanHoleCards, i0);
            });

            int aheadSum = 0;
            int tieSum = 0;
            int behindSum = 0;

            for (int i = 0; i < 52; i++)
            {
                aheadSum += aheadGlobal[i];
                tieSum += tieGlobal[i];
                behindSum += behindGlobal[i];
            }

            float total = aheadSum + tieSum + behindSum;
            return (aheadSum + tieSum / 2.0f) / total;
        }

        private static void CalculateTmp(int[] aheadGlobal, int[] tieGlobal, int[] behindGlobal, HoleCards heroHoleCards, HoleCards villanHoleCards, int i0)
        {
            bool[] knownCard = new bool[52];
            Card[] deck = new Card[52];

            for (int i = 0; i < 52; i++)
            {
                knownCard[i] = false;
                deck[i] = new Card(i);
            }

            knownCard[heroHoleCards.Card0.ToInt()] = true;
            knownCard[heroHoleCards.Card1.ToInt()] = true;

            knownCard[villanHoleCards.Card0.ToInt()] = true;
            knownCard[villanHoleCards.Card1.ToInt()] = true;

            int ahead = 0;
            int tie = 0;
            int behind = 0;

            HandStrengthCounter heroCounter = new HandStrengthCounter();
            HandStrengthCounter villanCounter = new HandStrengthCounter();

            heroCounter.AddCard(heroHoleCards.Card0);
            heroCounter.AddCard(heroHoleCards.Card1);

            villanCounter.AddCard(villanHoleCards.Card0);
            villanCounter.AddCard(villanHoleCards.Card1);

            Card[] board = new Card[5];

            if (!knownCard[i0])
            {
                board[0] = deck[i0];
                heroCounter.AddCard(deck[i0]);
                villanCounter.AddCard(deck[i0]);

                for (int i1 = i0 + 1; i1 < 49; i1++) // Flop 2
                {
                    if (knownCard[i1])
                        continue;

                    board[1] = deck[i1];
                    heroCounter.AddCard(deck[i1]);
                    villanCounter.AddCard(deck[i1]);

                    for (int i2 = i1 + 1; i2 < 50; i2++) // Flop 3
                    {
                        if (knownCard[i2])
                            continue;

                        board[2] = deck[i2];
                        heroCounter.AddCard(deck[i2]);
                        villanCounter.AddCard(deck[i2]);

                        for (int i3 = i2 + 1; i3 < 51; i3++) // Turn
                        {
                            if (knownCard[i3])
                                continue;

                            board[3] = deck[i3];
                            heroCounter.AddCard(deck[i3]);
                            villanCounter.AddCard(deck[i3]);

                            for (int i4 = i3 + 1; i4 < 52; i4++) // River
                            {
                                if (knownCard[i4])
                                    continue;

                                board[4] = deck[i4];
                                heroCounter.AddCard(deck[i4]);
                                villanCounter.AddCard(deck[i4]);

                                int result = heroCounter.CompareHandStrength(heroHoleCards, villanHoleCards, villanCounter, board);

                                if (result == HandStrengthCounter.AHEAD)
                                    ahead++;
                                else if (result == HandStrengthCounter.BEHIND)
                                    behind++;
                                else if (result == HandStrengthCounter.TIE)
                                    tie++;

                                heroCounter.RemoveCard(deck[i4]);
                                villanCounter.RemoveCard(deck[i4]);
                            }

                            heroCounter.RemoveCard(deck[i3]);
                            villanCounter.RemoveCard(deck[i3]);
                        }

                        heroCounter.RemoveCard(deck[i2]);
                        villanCounter.RemoveCard(deck[i2]);
                    }

                    heroCounter.RemoveCard(deck[i1]);
                    villanCounter.RemoveCard(deck[i1]);
                }

                heroCounter.RemoveCard(deck[i0]);
                villanCounter.RemoveCard(deck[i0]);
            }

            aheadGlobal[i0] = ahead;
            tieGlobal[i0] = tie;
            behindGlobal[i0] = behind;
        }
    }
}
