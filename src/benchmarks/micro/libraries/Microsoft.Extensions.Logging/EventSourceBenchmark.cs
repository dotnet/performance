// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.EventSource;
using MicroBenchmarks;

namespace Microsoft.Extensions.Logging
{
    [BenchmarkCategory(Categories.Libraries)]
    public class EventSourceLogger : LoggingBenchmarkBase
    {
        private ILogger _logger;
        private ILogger _noopLogger;

        private TestEventListener _listener;

        [Params(true, false)]
        public bool HasSubscribers { get; set; } = true;

        [Params(true, false)]
        public bool Json { get; set; } = false;

        [Benchmark]
        [MemoryRandomization]
        public void NestedScopes_TwoMessages()
        {
            using (_logger.BeginScope("String scope"))
            {
                using (_logger.BeginScope(new SampleScope()))
                {
                    TwoArgumentErrorMessage(_logger, 1, "string", null);
                    TwoArgumentTraceMessage(_logger, 2, "string", null);
                }
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            if (HasSubscribers)
            {
                _listener = new TestEventListener(Json ? LoggingEventSource.Keywords.JsonMessage : LoggingEventSource.Keywords.FormattedMessage);
            }

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddEventSourceLogger());

            _logger = services.BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("Logger");

            services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ILoggerProvider, LoggerProvider<NoopLogger>>();

            _noopLogger = services.BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("Logger");
        }

        [GlobalCleanup]
        public void Cleanup() => _listener?.Dispose();

        private class TestEventListener : EventListener
        {
            private readonly EventKeywords _keywords;

            public TestEventListener(EventKeywords keywords) => _keywords = keywords;

            protected override void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource eventSource)
            {
                if (eventSource.Name == "Microsoft-Extensions-Logging")
                {
                    DisableEvents(eventSource);
                    EnableEvents(eventSource, EventLevel.Verbose, _keywords);
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventWrittenArgs)
            {
            }
        }
    }
}
