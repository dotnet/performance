using System.Text;

namespace BlazorLocalized.ClassLibrary.StringCreator.Creator
{
    public class Creator : ICreator
    {
        // default: basic Latin
        private int generationRangeLower = 32;
        private int generationRangeUpper = 126;

        private int exclusionRangeLower;
        private int exclusionRangeUpper;

        public string Generate(int length)
        {
            HashSet<int> exclude = new HashSet<int>();
            if (exclusionRangeLower <= exclusionRangeUpper)
            {
                exclude = Enumerable.Range(exclusionRangeLower, exclusionRangeUpper).ToHashSet();
            }
            var range = Enumerable.Range(generationRangeLower, generationRangeUpper).Where(i => !exclude.Contains(i));

            var rand = new Random();
            StringBuilder sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                int index = rand.Next(generationRangeLower, generationRangeUpper - exclude.Count);
                int random = range.ElementAt(index);
                sb.Append((char)random);
            }
            return sb.ToString();
        }

        public bool SetExclusionRange(int lower, int upper)
        {
            if (lower > upper)
                return false;
            exclusionRangeLower = lower;
            exclusionRangeUpper = upper;
            return true;
        }

        public bool SetGenerationRange(int lower, int upper)
        {
            if (lower > upper)
                return false;
            generationRangeLower = lower;
            generationRangeUpper = upper;
            return true;
        }
    }
}
