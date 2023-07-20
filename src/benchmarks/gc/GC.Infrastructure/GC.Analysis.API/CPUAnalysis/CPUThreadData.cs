namespace GC.Analysis.API
{
    public sealed class CPUThreadData
    {
        public string Method { get; set; }
        public float InclusiveCount { get; set; }
        public float ExclusiveCount { get; set; }
        public List<string> Callers { get; set; }
        public string Thread { get; set; }
        //public int HeapNumber { get; set; }
    }
}
