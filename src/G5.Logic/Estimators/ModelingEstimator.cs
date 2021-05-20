using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace G5.Logic.Estimators
{
    /// <summary>
    /// This is old (modelling) estimator. Estimates player model using Bayesian estimation.
    /// Than plays exploitevly to maximaze EV.
    /// </summary>
    public class ModelingEstimator : IActionEstimator
    {
        private OpponentModeling _opponentModeling;
        private DecisionMakingContext _dmContext;
        private PokerClient _pokerClient;

        public ModelingEstimator(OpponentModeling oppModelling, PokerClient pokerClient)
        {
            _opponentModeling = oppModelling;
            _pokerClient = pokerClient;
            _dmContext = new DecisionMakingContext();
        }

        public void Dispose()
        {
            if (_dmContext != null)
            {
                _dmContext.Dispose();
                _dmContext = null;
            }
        }

        private EstimatedAD getPlayerToActAD(Player playerToAct, BotGameState gameState)
        {
            if (gameState.getStreet() == Street.PreFlop)
            {
                var preFlopParams = new PreFlopParams(
                    gameState.getTableType(),
                    playerToAct.PreFlopPosition,
                    gameState.getNumCallers(),
                    gameState.getNumBets(),
                    gameState.numActivePlayers(),
                    playerToAct.LastAction,
                    gameState.isPlayerInPosition(gameState.getPlayerToActInd()));

                return playerToAct.GetAD(preFlopParams);
            }
            else
            {
                var round = playerToAct.Round();
                ActionType prevAction = (round == 0) ? playerToAct.PrevStreetAction : playerToAct.LastAction;

                var postFlopParams = new PostFlopParams(
                    gameState.getTableType(),
                    gameState.getStreet(),
                    round,
                    prevAction,
                    gameState.getNumBets(),
                    gameState.isPlayerInPosition(gameState.getPlayerToActInd()),
                    gameState.numActivePlayers());

                return playerToAct.GetAD(postFlopParams);
            }
        }

        private bool canNextPlayerRaise(Player playerToAct, BotGameState gameState)
        {
            return (gameState.getNumBets() < 4) &&
                gameState.getAmountToCall() < playerToAct.Stack &&
                gameState.numActiveNonAllInPlayers() > 1;
        }

        private void getPlayerToActAD(ref float betRaiseProb, ref float checkCallProb, BotGameState gameState)
        {
            var playerToAct = gameState.getPlayerToAct();
            EstimatedAD ad = getPlayerToActAD(playerToAct, gameState);
            Debug.Assert(ad.PriorSamples > 0);

            betRaiseProb = ad.BetRaise.Mean;
            checkCallProb = ad.CheckCall.Mean;

            if (!canNextPlayerRaise(playerToAct, gameState))
            {
                checkCallProb += betRaiseProb;
                betRaiseProb = 0.0f;
            }

            Console.WriteLine($"{playerToAct.Name} model stats: BR {betRaiseProb.ToString("f2")}; CC {checkCallProb.ToString("f2")};" +
                $" FO {(1 - betRaiseProb - checkCallProb).ToString("f2")} [prior smpls:{ad.PriorSamples}, updates:{ad.UpdateSamples}]");
        }

        void IActionEstimator.newAction(ActionType actionType, BotGameState gameState)
        {
            float betRaiseProb = 0.0f;
            float checkCallProb = 0.0f;
            getPlayerToActAD(ref betRaiseProb, ref checkCallProb, gameState);

            gameState.getPlayerToAct().CutRange(actionType,
                gameState.getStreet(),
                gameState.getBoard(),
                betRaiseProb,
                checkCallProb, _dmContext);
        }

        void IActionEstimator.newStreet(BotGameState gameState)
        {
            if (gameState.getStreet() == Street.Flop)
                DecisionMakingDll.GameContext_NewFlop(_dmContext, gameState.getBoard(), gameState.getHeroHoleCards());
        }

        void IActionEstimator.newHand(BotGameState gameState)
        {
            Parallel.ForEach(gameState.getPlayers(), (player) =>
            {
                if (_opponentModeling != null)
                    player.UpdateModel(_opponentModeling.estimatePlayerModel(player.Name, _pokerClient));
            });

            /*foreach (Player player in _players)
            {
                player.UpdateModel(_opponentModeling.estimatePlayerModel(player.Name, _pokerClient));
            }*/
        }

        void IActionEstimator.estimateEV(out float checkCallEV, out float betRaiseEV, BotGameState gameState)
        {
            DecisionMakingDll.Holdem_EstimateEV(out checkCallEV, out betRaiseEV, gameState.getButtonInd(),
                gameState.getHeroInd(),
                gameState.getHeroHoleCards(),
                gameState.getPlayers(),
                gameState.getBoard(),
                gameState.getStreet(),
                gameState.getNumBets(),
                gameState.getNumCallers(),
                gameState.getBigBlingSize(), _dmContext);
        }
    }
}
