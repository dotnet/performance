// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using BenchmarkDotNet.Attributes;

namespace System.ConsoleTests
{
    /// <summary>
    /// Perf tests for Console are chosen based on which functions have PAL code. They are:
    /// 
    /// - OpenStandardInput, OpenStandardOutput, OpenStandardError
    /// - ForegroundColor, BackgroundColor, ResetColor
    /// </summary>
    public class Perf_Console
    {
        const int StreamInnerIterations = 50;
        const int ColorInnerIterations = 1000;

        private Stream[] _streams;

        [GlobalSetup(Target = nameof(OpenStandardInput) + "," + nameof(OpenStandardOutput) + "," + nameof(OpenStandardError))]
        public void SetupStreams() => _streams = new Stream[StreamInnerIterations * 4];

        [GlobalCleanup(Target = nameof(OpenStandardInput) + "," + nameof(OpenStandardOutput) + "," + nameof(OpenStandardError))]
        public void CleanupStreams()
        {
            foreach (var stream in _streams)
                stream.Dispose();
        }
        
        [Benchmark]
        public void OpenStandardInput()
        {
            Stream[] streams = _streams;
            
            for (int i = 0; i < StreamInnerIterations; i++)
            {
                streams[0 + i * 4] = Console.OpenStandardInput(); streams[1 + i * 4] = Console.OpenStandardInput();
                streams[2 + i * 4] = Console.OpenStandardInput(); streams[3 + i * 4] = Console.OpenStandardInput();
            }
        }

        [Benchmark]
        public void OpenStandardOutput()
        {
            Stream[] streams = _streams;
            
            for (int i = 0; i < StreamInnerIterations; i++)
            {
                streams[0 + i * 4] = Console.OpenStandardOutput(); streams[1 + i * 4] = Console.OpenStandardOutput();
                streams[2 + i * 4] = Console.OpenStandardOutput(); streams[3 + i * 4] = Console.OpenStandardOutput();
            }
        }

        [Benchmark]
        public void OpenStandardError()
        {
            Stream[] streams = _streams;
            
            for (int i = 0; i < StreamInnerIterations; i++)
            {
                streams[0 + i * 4] = Console.OpenStandardError(); streams[1 + i * 4] = Console.OpenStandardError();
                streams[2 + i * 4] = Console.OpenStandardError(); streams[3 + i * 4] = Console.OpenStandardError();
            }
        }

        [Benchmark]
        public void ForegroundColor()
        {
            for (int i = 0; i < ColorInnerIterations; i++)
            {
                Console.ForegroundColor = ConsoleColor.Black; Console.ForegroundColor = ConsoleColor.Blue;
                Console.ForegroundColor = ConsoleColor.Cyan; Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.DarkGray; Console.ForegroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.DarkGreen; Console.ForegroundColor = ConsoleColor.White;
            }
        }
        
        [GlobalCleanup(Target = nameof(ForegroundColor))]
        public void ForegroundColorCleanup() => Console.ResetColor();

        [Benchmark]
        public void BackgroundColor()
        {
            for (int i = 0; i < ColorInnerIterations; i++)
            {
                Console.BackgroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Blue;
                Console.BackgroundColor = ConsoleColor.Cyan; Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.BackgroundColor = ConsoleColor.DarkGray; Console.BackgroundColor = ConsoleColor.Red;
                Console.BackgroundColor = ConsoleColor.DarkGreen; Console.BackgroundColor = ConsoleColor.White;
            }
        }

        [GlobalCleanup(Target = nameof(BackgroundColor))]
        public void BackgroundColorCleanup() => Console.ResetColor();

        [Benchmark]
        public void ResetColor()
        {
            for (int i = 0; i < ColorInnerIterations; i++)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed; Console.BackgroundColor = ConsoleColor.Cyan;
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkRed; Console.BackgroundColor = ConsoleColor.Cyan;
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkRed; Console.BackgroundColor = ConsoleColor.Cyan;
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkRed; Console.BackgroundColor = ConsoleColor.Cyan;
                Console.ResetColor();
            }
        }
    }
}
