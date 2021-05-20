namespace G5.Logic
{
    public class StatValue
    {
        private int _positive;
        private int _total;

        public StatValue()
        {
        }

        public StatValue(int p, int t)
        {
            _positive = p;
            _total = t;
        }

        public int TotalSamples
        {
            get { return _total; }
        }

        public int PositiveSamples
        {
            get { return _positive; }
        }

        public void AddSample(bool isPositive)
        {
            _total++;

            if (isPositive)
                _positive++;
        }

        public void AddSample(int numPositive)
        {
            _total++;
            _positive += numPositive;
        }

        public void Append(StatValue sv)
        {
            _total += sv._total;
            _positive += sv._positive;
        }

        public float ToFloat()
        {
            return (_total > 0) ? (_positive / (float)_total) : 0.0f;
        }

        public override string ToString()
        {
            return ToFloat().ToString("f2") + " (" + _total + ")";
        }
    }
}
