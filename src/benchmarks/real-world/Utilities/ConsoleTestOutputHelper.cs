using System;

namespace RealWorld
{
    public class ConsoleTestOutputHelper : ITestOutputHelper
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
