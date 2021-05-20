using System;
using System.Diagnostics;
using TensorFlow;


namespace G5.Logic.Estimators
{
    public class DLEstimator : IActionEstimator
    {
        private TFGraph _graph;
        private TFSession _session;

        public DLEstimator(String modelPath)
        {
            _graph = new TFGraph();
            _graph.Import(new TFBuffer(System.IO.File.ReadAllBytes(modelPath)));
            _session = new TFSession(_graph);
        }

        void IDisposable.Dispose()
        {
            if (_session != null)
            {
                _session.Dispose();
                _session = null;
            }

            if (_graph != null)
            {
                _graph.Dispose();
                _graph = null;
            }
        }

        void IActionEstimator.newAction(ActionType actionType, BotGameState gameState)
        {
            // Nothing here
        }

        void IActionEstimator.newHand(BotGameState gameState)
        {
            // Nothing here
        }

        void IActionEstimator.newStreet(BotGameState gameState)
        {
            // Nothing here
        }

        private void cardToVec(float[] dst, int dstInd, Card card)
        {
            if (card.suite == Card.Suite.Unknown)
                return;

            if (card.rank == Card.Rank.Unknown)
                return;

            dst[(int)card.suite] = 1;
            dst[4 + (int)card.rank - 2] = 1;
        }

        private readonly int cardsTensorLen = 119;

        private float[] cardsToVector(BotGameState gameState)
        {
            int cardBits = (4 + 13);
            float[] res = new float[cardBits * (2 + 5)];

            int ind = 0;

            cardToVec(res, ind, gameState.getHeroHoleCards().Card0); ind += cardBits;
            cardToVec(res, ind, gameState.getHeroHoleCards().Card1); ind += cardBits;

            foreach (var card in gameState.getBoard().Cards)
            {
                cardToVec(res, ind, card);
                ind += cardBits;
            }

            return res;
        }

        void IActionEstimator.estimateEV(out float checkCallEV, out float betRaiseEV, BotGameState gameState)
        {
            checkCallEV = 0;
            betRaiseEV = 0;

            var cardsData = cardsToVector(gameState);

            // Prepare the inputs from game state 
            var cards = TensorFlow.TFTensor.FromBuffer(new TFShape(new long[] { 1, 119, 1 }), cardsData, 0, 7);

            // Set the inputs to the network
            /*var runner = _session.GetRunner();
            runner.AddInput(_graph["x"][0], x);

            // Run the network
            runner.Fetch(_graph["y"][0]);
            var tensorOutput = runner.Run();

            // Get the outputs (estimated EV)
            var floatOutput = (float[])tensorOutput[0].GetValue(jagged: false);
            checkCallEV = floatOutput[0];
            betRaiseEV = floatOutput[1];*/
        }
    }
}
