// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System;

namespace Benchmark
{
    [BenchmarkCategory(Categories.Libraries)]
    public class GetChildKeysTests
    {
        private ChainedConfigurationProvider _chainedConfig;
        private ChainedConfigurationProvider _chainedConfigEmpty; 
        private ChainedConfigurationProvider _chainedConfigWithSplitting;
        private ChainedConfigurationProvider _chainedConfigWithCommonPaths;
        private readonly string[] _emptyArray = Array.Empty<string>();

        [GlobalSetup]
        public void SetupBasic()
        {
            var emptyKeys = new Dictionary<string, string>() { };
            for (int i = 0; i < 1000; i++)
            {
                emptyKeys.Add(new string(' ', i), string.Empty);
            }

            var inputKeys = new Dictionary<string, string>() { };
            for (int i = 1000; i < 2000; i++)
            {
                inputKeys.Add(i.ToString(), string.Empty);
            }

            var splittingKeys = new Dictionary<string, string>() { };
            for (int i = 1000; i < 2000; i++)
            {
                splittingKeys.Add("a:" + i.ToString(), string.Empty);
            }

            var keysWithCommonPaths = new Dictionary<string, string>() { };
            for (int i = 1000; i < 2000; i++)
            {
                keysWithCommonPaths.Add("a:b:c" + i.ToString(), string.Empty);
            }

            _chainedConfigEmpty = new ChainedConfigurationProvider(new ChainedConfigurationSource
            {
                Configuration = new ConfigurationBuilder()
                    .Add(new MemoryConfigurationSource { InitialData = emptyKeys })
                    .Build(),
                ShouldDisposeConfiguration = false,
            });

            _chainedConfig = new ChainedConfigurationProvider(new ChainedConfigurationSource
            {
                Configuration = new ConfigurationBuilder()
                    .Add(new MemoryConfigurationSource { InitialData = inputKeys })
                    .Build(),
                ShouldDisposeConfiguration = false,
            });

            _chainedConfigWithSplitting = new ChainedConfigurationProvider(new ChainedConfigurationSource
            {
                Configuration = new ConfigurationBuilder()
                    .Add(new MemoryConfigurationSource { InitialData = splittingKeys })
                    .Build(),
                ShouldDisposeConfiguration = false,
            });

            _chainedConfigWithCommonPaths = new ChainedConfigurationProvider(new ChainedConfigurationSource
            {
                Configuration = new ConfigurationBuilder()
                    .Add(new MemoryConfigurationSource { InitialData = keysWithCommonPaths })
                    .Build(),
                ShouldDisposeConfiguration = false,
            });
        }

        [GlobalCleanup(Targets = new[] {
            nameof(AddChainedConfigurationNoDelimiter),
            nameof(AddChainedConfigurationEmpty),
            nameof(AddChainedConfigurationWithSplitting),
            nameof(AddChainedConfigurationWithCommonPaths)
        })]
        public void CleanupBasic()
        {
            _chainedConfig.Dispose();
            _chainedConfigEmpty.Dispose();
            _chainedConfigWithSplitting.Dispose();
            _chainedConfigWithCommonPaths.Dispose();
        }

        [Benchmark]
        public void AddChainedConfigurationNoDelimiter() => _chainedConfig.GetChildKeys(_emptyArray, null);

        [Benchmark]
        public void AddChainedConfigurationEmpty() => _chainedConfigEmpty.GetChildKeys(_emptyArray, null);

        [Benchmark]
        public void AddChainedConfigurationWithSplitting() => _chainedConfigWithSplitting.GetChildKeys(_emptyArray, null);

        [Benchmark]
        public void AddChainedConfigurationWithCommonPaths() => _chainedConfigWithCommonPaths.GetChildKeys(_emptyArray, null);
    }
}
