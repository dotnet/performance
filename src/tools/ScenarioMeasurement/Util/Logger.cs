// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

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
