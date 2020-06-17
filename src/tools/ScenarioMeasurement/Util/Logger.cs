using System;
using System.Collections.Generic;
using System.Text;

namespace ScenarioMeasurement
{
    public class Logger
    {
        public Logger(string fileName)
        {

        }
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void LogIterationHeader(string message)
        {
            Console.WriteLine($"=============== {message} ================ ");
        }

        public void LogStepHeader(string message)
        {
            Console.WriteLine($"***{message}***");
        }

        public void LogVerbose(string message)
        {
            Console.WriteLine(message);
        }
    }
}
