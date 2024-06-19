namespace GC.Analysis.API
{
    public static class Statistics
    {
        public static double Percentile(this IEnumerable<double> seq, double percentile)
        {
            if (seq.Count() == 0)
            {
                return double.NaN;
            }

            var elements = seq.ToArray();
            Array.Sort(elements);
            double realIndex = percentile * (elements.Length - 1);
            int index = (int)realIndex;
            double frac = realIndex - index;
            if (index + 1 < elements.Length)
                return elements[index] * (1 - frac) + elements[index + 1] * frac;
            else
                return elements[index];
        }

        public static double StandardDeviation(this IEnumerable<double> doubleList)
        {
            double average = doubleList.Average();
            double sumOfDerivation = 0;
            foreach (double value in doubleList)
            {
                sumOfDerivation += (value) * (value);
            }
            double sumOfDerivationAverage = sumOfDerivation / (doubleList.Count() - 1);
            return Math.Sqrt(sumOfDerivationAverage - (average * average));
        }
    }
}
