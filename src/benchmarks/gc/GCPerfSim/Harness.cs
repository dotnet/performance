public static class Harness
{
    public static void Main(string[] args)
    {
        using var listener = new GCEventListener();
        MemoryAlloc.Test(args);
    }
}