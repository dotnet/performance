using System.Text;

namespace BlazorLocalized.ClassLibrary.StringCreator.Creator
{
    internal class SampleStringsModel
    {
        public StringSet? Universal { get; set; }
        public StringSet? LatinBasic { get; set; }
        public StringSet? LatinWithSupplement { get; set; }
        public StringSet? LatinExtended { get; set; }
    }

    internal class StringSet
    {
        private Random _rand;
        public string[]? Length100 { get; set; }
        public string[]? Length200 { get; set; }
        public string[]? Length500 { get; set; }

        public StringSet()
        {
            _rand = new Random();
        }

        public string? GetString(int length)
        {
            int index = _rand.Next(0, 3);
            if (length <= 100)
                return Length100?[index].Take(length)?.ToString();
            if (length <= 200)
                return Length200?[index].Take(length)?.ToString();
            if (length <= 500)
                return Length500?[index].Take(length)?.ToString();
            int bigSetsCnt = length / 500;
            StringBuilder sb = new(Length500?[index], length);
            for (int i = 0; i < bigSetsCnt; i++)
            {
                index = _rand.Next(0, 3);
                sb.Append(Length500?[index]);
            }
            return sb.ToString();
        }
    }
}
