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

        public static IEnumerable<double> RemoveOutliers(IEnumerable<double> collection)
        {
            if (!collection.Any())
            {
                return Array.Empty<double>();
            }
            double[] validCollection = collection
                .Where(x => !double.IsNaN(x) && !double.IsInfinity(x))
                .ToArray();
            // Calculate Q1 (25th percentile) and Q3 (75th percentile)
            double q1 = GC.Analysis.API.Statistics.Percentile(validCollection, 0.25);
            double q3 = GC.Analysis.API.Statistics.Percentile(validCollection, 0.75);

            // Calculate IQR (Interquartile Range)
            double iqr = q3 - q1;

            // Calculate bounds: [Q1 - 1.5*IQR, Q3 + 1.5*IQR]
            double lowerBound = q1 - 1.5 * iqr;
            double upperBound = q3 + 1.5 * iqr;

            // Filter out outliers
            return GoodLinq.Where(collection, x => x >= lowerBound && x <= upperBound);
        }
    }
}
