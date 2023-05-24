using System;
using System.Diagnostics;

namespace emptycsconsoletemplate
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World2!");
            if((OperatingSystem.IsWindows() || OperatingSystem.IsLinux()))
            {
                Console.WriteLine($"Process Affinity: {Process.GetCurrentProcess().ProcessorAffinity}, mask: {Convert.ToString((int)Process.GetCurrentProcess().ProcessorAffinity, 2)}");
            }
        }
    }
}
