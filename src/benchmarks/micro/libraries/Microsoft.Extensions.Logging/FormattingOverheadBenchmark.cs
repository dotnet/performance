// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using MicroBenchmarks;

namespace Microsoft.Extensions.Logging
{
    [BenchmarkCategory(Categories.Libraries)]
    public class FormattingOverhead : LoggingBenchmarkBase
    {
        private ILogger _logger;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ILoggerProvider, LoggerProvider<FormattingLogger>>();
            _logger = services.BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("Logger");
        }

        [Benchmark]
        public void TwoArguments_DefineMessage()
        {
            TwoArgumentErrorMessage(_logger, 1, "string", Exception);
        }

        [Benchmark]
        public void FourArguments_DefineMessage()
        {
            FourArgumentErrorMessage(_logger, 1, "string", 2, "string", Exception);
        }

        [Benchmark]
        public void NoArguments()
        {
            _logger.LogError(Exception, "Message");
        }

        [Benchmark]
        public void TwoArguments()
        {
            _logger.LogError(Exception, "Message {Argument1} {Argument2}", 1, "string");
        }

        [Benchmark]
        public void FourArguments_EnumerableArgument()
        {
            _logger.LogError(Exception, "Message {Argument1} {Argument2} {Argument3} {Argument4}", 1, "string", new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 1);
        }
    }
}
