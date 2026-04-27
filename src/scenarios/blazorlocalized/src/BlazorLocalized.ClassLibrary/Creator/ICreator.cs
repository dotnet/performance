namespace BlazorLocalized.ClassLibrary.StringCreator.Creator
{
    internal interface ICreator
    {
        public string Generate(int length);
        public bool SetGenerationRange(int lower, int upper);
        public bool SetExclusionRange(int lower, int upper);
    }
}
