using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace G5.Logic
{
    public class HistDistribution
    {
        private const float SAMPLE_SIGMA = 0.03f; // 0.03f

        private float[] distribution;

        public HistDistribution(HistDistribution dist)
        {
            distribution = new float[dist.distribution.Length];

            for (int i = 0; i < dist.distribution.Length; i++)
                distribution[i] = dist.distribution[i];
        }

        public HistDistribution(int n)
        {
            distribution = new float[n];

            for (int i = 0; i < distribution.Length; i++)
                distribution[i] = 0;
        }

        private static float Gauss(float x, float x0, float sigma)
        {
            var A = 1 / (sigma * Math.Sqrt(2 * Math.PI));
            var X = (x - x0) / sigma;

            var Y = A * Math.Exp(-0.5 * X * X);
            return (float)Y;
        }

        public void AddSample(float value)
        {
            var step = 1.0f / distribution.Length;
            var left = (int)((value - 3 * SAMPLE_SIGMA) * distribution.Length);
            var right = (int)((value + 3 * SAMPLE_SIGMA) * distribution.Length);

            left = Math.Max(0, left);
            right = Math.Min(distribution.Length - 1, right);

            for (int i = left; i <= right; i++)
            {
                float x = (i + 0.5f) * step;
                distribution[i] += Gauss(x, value, SAMPLE_SIGMA);
            }
        }

        public void Update(bool positive)
        {
            var step = 1.0f / distribution.Length;

            for (var i = 0; i < distribution.Length; i++)
            {
                var x = (i + 0.5f) * step;
                distribution[i] *= (positive) ? x : (1 - x);
            }

            Normalize();
        }

        public void Normalize()
        {
            var sum = 0.0f;

            for (int i = 0; i < distribution.Length; i++)
                sum += distribution[i];

            for (int i = 0; i < distribution.Length; i++)
                distribution[i] = (sum != 0) ? (distribution[i] / sum) : (1.0f / distribution.Length);
        }

        public float ExpectedValue()
        {
            var step = 1.0f / distribution.Length;
            var sum = 0.0f;

            for (var i = 0; i < distribution.Length; i++)
            {
                float x = (i + 0.5f) * step;
                sum += distribution[i] * x;
            }

            return sum;
        }

        public float StandardDeviation()
        {
            float exp = ExpectedValue();

            float step = 1.0f / distribution.Length;
            float variance = 0;

            for (int i = 0; i < distribution.Length; i++)
            {
                float x = (i + 0.5f) * step;
                variance += distribution[i] * (x - exp) * (x - exp);
            }

            return (float)Math.Sqrt(variance);
        }

        public GaussianDistribution FitGaussian()
        {
            return new GaussianDistribution(ExpectedValue(), StandardDeviation());
        }

        public float Difference(float val2, float val1)
        {
            float sd = StandardDeviation();
            return Math.Abs(val2 - val1) / (4 * sd);
        }

        public GaussianDistribution Difference(GaussianDistribution val2, GaussianDistribution val1)
        {
            float sd = StandardDeviation();
            return val2.AbsSub(val1).Scale(1 / (4 * sd));
        }

        override public string ToString()
        {
            return ExpectedValue().ToString("f2") + " +- " + StandardDeviation().ToString("f2");
        }
    }
}
