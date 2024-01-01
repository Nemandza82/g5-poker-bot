using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;

namespace G5.Logic
{
    /// <summary>
    /// Models opponents for given limit (big blind size).
    /// Tracks statistics for those parameters; uses the statistics and bayes rule to estimate missing
    /// parameters for new players, and 'not so new' players but for obscure situations.
    /// </summary>
    public class OpponentModeling
    {
        public class Options
        {
            /// <summary>
            /// Number of recent hands used to calculate opponent model. Eg 15, 3000..
            /// </summary>
            public int recentHandsCount;

            /// <summary>
            /// Number of bins used when contructing in prior distribution (100).
            /// </summary>
            public int priorNumBins;

            /// <summary>
            /// Minimum number of samples if statistics is going to be used in contructing a prior (20).
            /// </summary>
            public int minSamples;

            /// <summary>
            /// Maximum number of similar players used when estimating prior distribution (130).
            /// </summary>
            public int maxSimilarPlayers;

            /// <summary>
            /// Maximum difference between players in order for player statistics to be used in constructing a prior (0.3).
            /// </summary>
            public float maxDifference;

            /// <summary>
            /// Maximum sigma value of base stats if player statistics is going to be used in contructing a prior (0.08).
            /// </summary>
            public float maxBaseStatsSigma;

            public Options()
            {
                priorNumBins = 100;
                minSamples = 20;
                maxBaseStatsSigma = 0.08f;
                maxSimilarPlayers = 130;
                maxDifference = 0.3f;
                recentHandsCount = 15;
            }
        };

        private class BaseModel
        {
            public GaussianDistribution VPIP;
            public GaussianDistribution PFR;
            public GaussianDistribution WTP;
            public GaussianDistribution Aggression;
        };

        private Options _options;
        public TableType TableType { get; private set; }

        private Random _random = new Random();
        private HistDistribution _vpipPrior;
        private HistDistribution _aggressionPrior;
        private HistDistribution _pfrPrior;
        private HistDistribution _wtpPrior;

        private List<PlayerStats> _fullStatsList;
        private List<BaseModel> _baseModels;
        private Dictionary<PlayerKey, List<Hand>> _recentHandsList;

        public List<PlayerStats> FullStatsList
        {
            get { return _fullStatsList; }
        }
        
        public OpponentModeling(string fullStatListFileName, TableType tableType, Options options)
        {
            _options = options;
            TableType = tableType;

            _fullStatsList = PlayerStats.loadStatsList(fullStatListFileName);
            _recentHandsList = new Dictionary<PlayerKey, List<Hand>>();

            initBaseStats();
        }

        public PlayerStats getPlayerStats(string playerName, PokerClient client)
        {
            return _fullStatsList.SingleOrDefault(i => i.PlayerName == playerName && i.Client == client);
        }

        /// <summary>
        /// TO set hand list from database
        /// var handDtos = Database.Hands.GetHands(client.ToString(), GameType.HoldEm.ToString(), Limit,
        ///    playerName, null, null, _recentHandsCount);
        /// handList = handDtos.Select(h => h.ToHand()).ToList();
        /// </summary>
        private void setRecentHandList(List<Hand> list, string playerName, PokerClient client)
        {
            _recentHandsList[new PlayerKey { PlayerName = playerName, Client = client }] = list;
        }

        private List<Hand> getRecentHandList(string playerName, PokerClient client)
        {
            var playerKey = new PlayerKey { PlayerName = playerName, Client = client };
            
            if (_recentHandsList.ContainsKey(playerKey))
            {
                return _recentHandsList[playerKey];
            }
            else
            {
                return null;
            }
        }

        /**
          * {
          *   var progress = new Progress<int>(percent =>
          *    {
          *      textBox1.Text = percent + "%";
          *    });
          *
          *    // DoProcessing is run on the thread pool.
          *    await Task.Run(() => DoProcessing(progress));
          *    textBox1.Text = "Done!";
          *  }
          *  
          *  public void DoProcessing(IProgress<int> progress)
          */
        public PlayerModel estimatePlayerModel(PlayerStats playerStats)
        {
            BaseModel baseModel = estimateBaseModel(playerStats);

            DifferencePair[] similarOppPreFlop = getSimilarOpponents_PreFlop(baseModel.VPIP, baseModel.PFR);
            DifferencePair[] similarOppPostFlop = getSimilarOpponents_PostFlop(baseModel.Aggression, baseModel.WTP);

            var allPreFlopParams = PreFlopParams.getAllParams(TableType);
            var preFlopAD = new EstimatedAD[allPreFlopParams.Count];

            for (int i = 0; i < allPreFlopParams.Count; i++)
            {
                preFlopAD[i] = estimateADPreFLop(playerStats, similarOppPreFlop, allPreFlopParams[i]);
            }

            var allPostFlopParams = PostFlopParams.getAllParams(TableType);
            var postFlopAD = new EstimatedAD[allPostFlopParams.Count];

            for (int i = 0; i < allPostFlopParams.Count; i++)
            {
                postFlopAD[i] = estimateADPostFlop(playerStats, similarOppPostFlop, allPostFlopParams[i]);
            }

            return new PlayerModel(TableType, baseModel.VPIP, baseModel.PFR, baseModel.WTP, baseModel.Aggression, preFlopAD, postFlopAD);
        }

        public PlayerModel estimatePlayerModel(string playerName, PokerClient client)
        {
            var handList = getRecentHandList(playerName, client);
            var playerStats = PlayerStats.createStatsFromHandList(handList, playerName, client, TableType);
            return estimatePlayerModel(playerStats);
        }

        /// <summary>
        /// Adds new hand to stats list.
        /// </summary>
        public void addHand(Hand hand)
        {
            foreach (var playerName in hand.PlayersNames)
            {
                // Update FullSstatsList
                var playerStats = _fullStatsList.Find(s => s.PlayerName == playerName && s.Client == hand.Client);

                if (playerStats == null)
                {
                    playerStats = new PlayerStats(playerName, hand.Client, TableType);
                    _fullStatsList.Add(playerStats);
                    //_baseModels.Add(estimateBaseModel(playerStats));
                }

                playerStats.increment(hand);

                // updating _recentHandsList
                var playerKey = new PlayerKey { PlayerName = playerName, Client = hand.Client };
                var handList = new List<Hand>();

                if (_recentHandsList.ContainsKey(playerKey))
                {
                    handList = _recentHandsList[playerKey];
                }
                else
                {
                    _recentHandsList.Add(playerKey, handList);
                }
                
                while (handList.Count >= _options.recentHandsCount)
                    handList.RemoveAt(0);

                handList.Add(hand);
            }
        }

        private void initBaseStats()
        {
            _vpipPrior = createVPIPPrior();
            _aggressionPrior = createAggressionPrior();
            _pfrPrior = createPFRPrior();
            _wtpPrior = createWTPPrior();

            _baseModels = new List<BaseModel>();

            foreach (var stats in _fullStatsList)
                _baseModels.Add(null);

            // Using prior distributions to estimate key stats for each player (in parallel)
            Parallel.ForEach(Partitioner.Create(0, _fullStatsList.Count), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    _baseModels[i] = estimateBaseModel(_fullStatsList[i]);
                }
            });

            /* // Not parallel
            foreach (var stats in _fullStatsList)
            {
                _baseModels[i] = estimateBaseStats(stats);
            }*/
        }

        private BaseModel estimateBaseModel(PlayerStats stats)
        {
            BaseModel baseModel = new BaseModel();
            baseModel.VPIP = estimateGaussian(_vpipPrior, stats.VPIP.PositiveSamples, stats.VPIP.TotalSamples);

            int positive;
            int total;

            stats.calculateAggression(out positive, out total);
            baseModel.Aggression = estimateGaussian(_aggressionPrior, positive, total);

            stats.calculatePFR(out positive, out total);
            baseModel.PFR = estimateGaussian(_pfrPrior, positive, total);

            stats.calculateWTP(out positive, out total);
            baseModel.WTP = estimateGaussian(_wtpPrior, positive, total);

            return baseModel;
        }

        /// <summary>
        /// Creates VPIP prior distribution according to statistics.
        /// </summary>
        private HistDistribution createVPIPPrior()
        {
            var dist = new HistDistribution(_options.priorNumBins);

            foreach (var stats in _fullStatsList)
            {
                if (stats.VPIP.TotalSamples > _options.minSamples)
                    dist.AddSample(stats.VPIP.ToFloat());
            }

            dist.Normalize();
            return dist;
        }

        /// <summary>
        /// Creates PFR prior distribution according to statistics.
        /// </summary>
        private HistDistribution createPFRPrior()
        {
            var dist = new HistDistribution(_options.priorNumBins);

            foreach (PlayerStats stats in _fullStatsList)
            {
                int positive;
                int total;
                stats.calculatePFR(out positive, out total);

                if (total > _options.minSamples)
                    dist.AddSample(positive / (float)total);
            }

            dist.Normalize();
            return dist;
        }

        /// <summary>
        /// Creates Aggression prior distribution according to statistics.
        /// </summary>
        private HistDistribution createAggressionPrior()
        {
            var dist = new HistDistribution(_options.priorNumBins);

            foreach (PlayerStats stats in _fullStatsList)
            {
                int raiseCount;
                int totalCount;
                stats.calculateAggression(out raiseCount, out totalCount);

                if (totalCount > _options.minSamples)
                    dist.AddSample(raiseCount / (float)totalCount);
            }

            dist.Normalize();
            return dist;
        }

        /// <summary>
        /// Creates WTP (Willingnes to Play) prior distribution according to statistics.
        /// </summary>
        private HistDistribution createWTPPrior()
        {
            var dist = new HistDistribution(_options.priorNumBins);

            foreach (PlayerStats stats in _fullStatsList)
            {
                int positive;
                int total;
                stats.calculateWTP(out positive, out total);

                if (total > _options.minSamples)
                    dist.AddSample(positive / (float)total);
            }

            dist.Normalize();
            return dist;
        }

        /// <summary>
        /// Estimates expected value of a probability distribution given prior and positive and total samples,
        /// using Bayes rule.
        /// </summary>
        /// <param name="prior">Prior probability distribution.</param>
        /// <param name="positiveSamples">Number of positive samples.</param>
        /// <param name="negSamples">Number of negative samples.</param>
        /// <returns>Estimated expected value.</returns>
        private void updateDistributionRandom(HistDistribution prior, int positiveSamples, int negSamples)
        {
            var pos = positiveSamples;
            var neg = negSamples;

            while (pos + neg > 0)
            {
                var ratio = pos / (double)(pos + neg);

                if ((pos > 0) && (_random.NextDouble() < ratio))
                {
                    prior.Update(true);
                    pos--;
                }
                else
                {
                    prior.Update(false);
                    neg--;
                }
            }

            Debug.Assert(pos == 0 && neg == 0);
        }

        private GaussianDistribution estimateGaussian(HistDistribution dist, int positiveSamples, int totalSamples)
        {
            var newDist = new HistDistribution(dist);

            // TODO: Privremeno
            if (totalSamples > 300)
            {
                positiveSamples = (300 * positiveSamples) / totalSamples;
                totalSamples = 300;
            }

            var pos = positiveSamples;
            var neg = totalSamples - positiveSamples;

            while (pos + neg > 0)
            {
                if (pos + neg <= 100)
                {
                    updateDistributionRandom(newDist, pos, neg);
                    pos = 0;
                    neg = 0;
                }
                else
                {
                    var newPos = (int) (100*(pos/(float) (pos + neg)));
                    var newNeg = (int) (100*(neg/(float) (pos + neg)));

                    updateDistributionRandom(newDist, newPos, newNeg);
                    pos -= newPos;
                    neg -= newNeg;
                }
            }

            Debug.Assert(pos == 0 && neg == 0);
            return newDist.FitGaussian();
        }

        private struct DifferencePair
        {
            public int Index;
            public float Difference;

            public DifferencePair(int ind, float val)
            {
                Index = ind;
                Difference = val;
            }

            public override string ToString()
            {
                return Index + " - " + Difference.ToString("f2");
            }
        };

        /// <summary>
        /// Sorts opponets of given player, according to similarity measure, by PreFlop similarity.
        /// </summary>
        /// <param name="playerStats">Given player statistics.</param>
        /// <returns>Sorted opponent list.</returns>
        private DifferencePair[] getSimilarOpponents_PreFlop(GaussianDistribution VPIP, GaussianDistribution PFR)
        {
            var diff = new DifferencePair[_baseModels.Count];

            for (int j = 0; j < _baseModels.Count; j++)
            {
                var vpipDiff = _vpipPrior.Difference(VPIP, _baseModels[j].VPIP);
                var pfrDiff = _pfrPrior.Difference(PFR, _baseModels[j].PFR);

                diff[j].Difference = (float)Math.Sqrt(vpipDiff.Mean * vpipDiff.Mean + pfrDiff.Mean * pfrDiff.Mean);
                diff[j].Index = j;
            }

            return diff.OrderBy(ep => ep.Difference).ToArray();
        }

        /// <summary>
        /// Sorts opponets of given player, according to similarity measure, by PostFlop similarity.
        /// </summary>
        /// <param name="playerStats"></param>
        /// <returns></returns>
        private DifferencePair[] getSimilarOpponents_PostFlop(GaussianDistribution aggression, GaussianDistribution WTP)
        {
            var diff = new DifferencePair[_baseModels.Count];

            for (int j = 0; j < _baseModels.Count; j++)
            {
                var aggDiff = _aggressionPrior.Difference(aggression, _baseModels[j].Aggression);
                var wtpDiff = _wtpPrior.Difference(WTP, _baseModels[j].WTP);

                diff[j].Difference = (float)Math.Sqrt(aggDiff.Mean * aggDiff.Mean + wtpDiff.Mean * wtpDiff.Mean);
                diff[j].Index = j;
            }

            return diff.OrderBy(ep => ep.Difference).ToArray();
        }

        private EstimatedAD estimateADPreFLop(PlayerStats playerStats, DifferencePair[] sortedOpponents, PreFlopParams preFlopParams)
        {
            Debug.Assert(playerStats != null);

            var priorBetRaise = new HistDistribution(_options.priorNumBins);
            var priorCheckCall = new HistDistribution(_options.priorNumBins);
            var priorFold = new HistDistribution(_options.priorNumBins);

            var cumulativeActionStats = new ActionStats();
            int k = 0;

            for (int i = 0; (i < sortedOpponents.Length) &&
                            (k < _options.maxSimilarPlayers) &&
                            (k == 0 || sortedOpponents[i].Difference < _options.maxDifference); i++)
            {
                int playerInd = sortedOpponents[i].Index;

                if (_baseModels[playerInd].VPIP.Sigma < _options.maxBaseStatsSigma)
                {
                    ActionStats similarOponentStats = _fullStatsList[playerInd].getPreFlopStats(preFlopParams);

                    if (similarOponentStats.totalSamples() > _options.minSamples)
                    {
                        priorBetRaise.AddSample(similarOponentStats.betRaiseProbability());
                        priorCheckCall.AddSample(similarOponentStats.checkCallProbability());
                        priorFold.AddSample(similarOponentStats.foldProbability());

                        k++;
                    }
                    else
                    {
                        cumulativeActionStats.append(similarOponentStats);
                    }

                    if (cumulativeActionStats.totalSamples() > _options.minSamples)
                    {
                        priorBetRaise.AddSample(cumulativeActionStats.betRaiseProbability());
                        priorCheckCall.AddSample(cumulativeActionStats.checkCallProbability());
                        priorFold.AddSample(cumulativeActionStats.foldProbability());

                        cumulativeActionStats.clear();
                        k++;
                    }
                }
            }

            priorBetRaise.Normalize();
            priorCheckCall.Normalize();
            priorFold.Normalize();

            // Update prior
            ActionStats startStats = playerStats.getPreFlopStats(preFlopParams);

            var estBetRaise = estimateGaussian(priorBetRaise, startStats.BetRaiseSamples, startStats.totalSamples());
            var estCheckCall = estimateGaussian(priorCheckCall, startStats.CheckCallSamples, startStats.totalSamples());
            var estFold = estimateGaussian(priorFold, startStats.FoldSamples, startStats.totalSamples());

            if (preFlopParams.ForcedAction())
            {
                var totalMean = estBetRaise.Mean + estCheckCall.Mean + estFold.Mean;
                var scale = 1.0f / totalMean;

                estBetRaise = estBetRaise.Scale(scale);
                estCheckCall = estCheckCall.Scale(scale);
                estFold = estFold.Scale(scale);
            }
            else
            {
                var totalMean = estBetRaise.Mean + estCheckCall.Mean;
                var scale = 1.0f / totalMean;

                estBetRaise = estBetRaise.Scale(scale);
                estCheckCall = estCheckCall.Scale(scale);
                estFold = new GaussianDistribution(0.0f, 0.0f);
            }

            return new EstimatedAD(estBetRaise, estCheckCall, estFold, k, startStats.totalSamples());
        }

        private EstimatedAD estimateADPostFlop(PlayerStats playerStats, DifferencePair[] sortedOpponents, PostFlopParams postFlopParams)
        {
            Debug.Assert(playerStats != null);

            var priorBetRaise = new HistDistribution(_options.priorNumBins);
            var priorCheckCall = new HistDistribution(_options.priorNumBins);
            var priorFold = new HistDistribution(_options.priorNumBins);

            var cumulativeActionsStats = new ActionStats();
            int k = 0;

            for (int i = 0; (i < sortedOpponents.Length) &&
                            (k < _options.maxSimilarPlayers) &&
                            (k == 0 || sortedOpponents[i].Difference < _options.maxDifference); i++)
            {
                int playerInd = sortedOpponents[i].Index;

                if (_baseModels[playerInd].Aggression.Sigma < _options.maxBaseStatsSigma)
                {
                    ActionStats similarOponentStats = _fullStatsList[playerInd].getPostFlopStats(postFlopParams);

                    if (similarOponentStats.totalSamples() > _options.minSamples)
                    {
                        priorBetRaise.AddSample(similarOponentStats.betRaiseProbability());
                        priorCheckCall.AddSample(similarOponentStats.checkCallProbability());
                        priorFold.AddSample(similarOponentStats.foldProbability());

                        k++;
                    }
                    else
                    {
                        cumulativeActionsStats.append(similarOponentStats);
                    }

                    if (cumulativeActionsStats.totalSamples() > _options.minSamples)
                    {
                        priorBetRaise.AddSample(cumulativeActionsStats.betRaiseProbability());
                        priorCheckCall.AddSample(cumulativeActionsStats.checkCallProbability());
                        priorFold.AddSample(cumulativeActionsStats.foldProbability());

                        cumulativeActionsStats.clear();
                        k++;
                    }
                }
            }

            priorBetRaise.Normalize();
            priorCheckCall.Normalize();
            priorFold.Normalize();

            // Update prior sa statistikom igraca
            ActionStats startStats = playerStats.getPostFlopStats(postFlopParams);

            var estBetRaise = estimateGaussian(priorBetRaise, startStats.BetRaiseSamples, startStats.totalSamples());
            var estCheckCall = estimateGaussian(priorCheckCall, startStats.CheckCallSamples, startStats.totalSamples());
            var estFold = estimateGaussian(priorFold, startStats.FoldSamples, startStats.totalSamples());

            if (postFlopParams.ForcedAction())
            {
                var totalMean = estBetRaise.Mean + estCheckCall.Mean + estFold.Mean;
                var scale = 1.0f / totalMean;

                estBetRaise = estBetRaise.Scale(scale);
                estCheckCall = estCheckCall.Scale(scale);
                estFold = estFold.Scale(scale);
            }
            else
            {
                var totalMean = estBetRaise.Mean + estCheckCall.Mean;
                var scale = 1.0f / totalMean;

                estBetRaise = estBetRaise.Scale(scale);
                estCheckCall = estCheckCall.Scale(scale);
                estFold = new GaussianDistribution(0.0f, 0.0f);
            }

            return new EstimatedAD(estBetRaise, estCheckCall, estFold, k, startStats.totalSamples());
        }
    }
}
