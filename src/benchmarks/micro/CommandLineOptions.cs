using System;
using System.Collections.Generic;

namespace MicroBenchmarks
{
    class CommandLineOptions
    {
        // Find and parse given parameter with expected int value, then remove it and its value from the list of arguments to then pass to BenchmarkDotNet
        // Throws ArgumentException if the parameter does not have a value or that value is not parsable as an int
        public static List<string> ParseAndRemoveIntParameter(List<string> argsList, string parameter, out int? parameterValue)
        {
            int parameterIndex = argsList.IndexOf(parameter);
            parameterValue = null;

            if (parameterIndex != -1)
            {
                if (parameterIndex + 1 < argsList.Count && Int32.TryParse(argsList[parameterIndex+1], out int parsedParameterValue))
                {
                    // remove --partition-count args
                    parameterValue = parsedParameterValue;
                    argsList.RemoveAt(parameterIndex+1);
                    argsList.RemoveAt(parameterIndex);
                }
                else
                {
                    throw new ArgumentException(String.Format("{0} must be followed by an integer", parameter));
                }
            }

            return argsList;
        }
    }
}