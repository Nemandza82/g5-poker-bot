using System;

namespace G5.Logic
{
    public struct GaussianDistribution
    {
        public float Mean { get; private set; }
        public float Sigma { get; private set; }

        public GaussianDistribution(float m, float s) : this()
        {
            Mean = m;
            Sigma = s;
        }

        public GaussianDistribution(int positive, int total) : this()
        {
            Mean = positive/(float) total;
            Sigma = (float) Math.Sqrt(Mean*(1.0f - Mean)/total);
        }

        private static float AddSigmas(float s1, float s2)
        {
            return (float) Math.Sqrt(s1*s1 + s2*s2);
        }

        public GaussianDistribution Scale(float scale)
        {
            return new GaussianDistribution(Mean*scale, Sigma*scale);
        }

        public GaussianDistribution Add(GaussianDistribution g)
        {
            float m = Mean + g.Mean;
            float s = AddSigmas(Sigma, g.Sigma);
            return new GaussianDistribution(m, s);
        }

        public GaussianDistribution Sub(GaussianDistribution g)
        {
            float m = Mean - g.Mean;
            float s = AddSigmas(Sigma, g.Sigma);
            return new GaussianDistribution(m, s);
        }

        public GaussianDistribution AbsSub(GaussianDistribution g)
        {
            float m = Math.Abs(Mean - g.Mean);
            float s = AddSigmas(Sigma, g.Sigma);
            return new GaussianDistribution(m, s);
        }

        override public string ToString()
        {
            return Mean.ToString("f2") + " +- " + Sigma.ToString("f2");
        }
    }
}
