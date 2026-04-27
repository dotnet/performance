
using System.Text.Json;

namespace BlazorLocalized.ClassLibrary.StringCreator.Creator
{
    public class StaticCreator : ICreator
    {
        private SampleStringsModel? _sampleStringsModel;
        private Range _generationRange;
        public StaticCreator()
        {
            using (StreamReader r = new StreamReader("SampleStrings.json"))
            {
                string jsonString = r.ReadToEnd();
                _sampleStringsModel = JsonSerializer.Deserialize<SampleStringsModel>(jsonString);
            }
        }
        public string Generate(int length)
        {
            string? str = null;
            switch (_generationRange)
            {
                case Range.Universal:
                    {
                        str = _sampleStringsModel?.Universal?.GetString(length);
                        break;
                    }
                case Range.LatinBasic:
                    {
                        str = _sampleStringsModel?.LatinBasic?.GetString(length);
                        break;
                    }
                case Range.LatinWithSupplement:
                    {
                        str = _sampleStringsModel?.LatinWithSupplement?.GetString(length);
                        break;
                    }
                case Range.LatinExtended:
                    {
                        str = _sampleStringsModel?.LatinExtended?.GetString(length);
                        break;
                    }
            }
            if (str == null)
                throw new Exception("Generated string is null");
            return str;
        }

        public bool SetExclusionRange(int lower, int upper)
        {
            throw new Exception("Exclusions are not supported in the static generation.");
        }

        public bool SetGenerationRange(int lower, int upper)
        {
            if (lower > upper)
                return false;
            if (lower < 126 && upper < 126)
            {
                _generationRange = Range.LatinBasic;
                return true;
            }
            if (lower < 255 && upper < 255)
            {
                _generationRange = Range.LatinWithSupplement;
                return true;
            }
            if (upper < 591 && lower < 591)
            {
                _generationRange = Range.LatinExtended;
                return true;
            }
            _generationRange = Range.Universal;
            return true;
        }
    }

    internal enum Range
    {
        Universal,
        LatinBasic,
        LatinWithSupplement,
        LatinExtended
    }
}
