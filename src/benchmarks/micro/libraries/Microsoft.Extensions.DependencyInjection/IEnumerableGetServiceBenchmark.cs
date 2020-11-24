// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Microsoft.Extensions.DependencyInjection
{
    [BenchmarkCategory(Categories.Libraries)]
    public class GetServiceIEnumerable : ServiceProviderEngineBenchmark
    {
        private IServiceProvider _serviceProvider;

        [GlobalSetup(Target = nameof(Transient))]
        public void SetupTransient() => Setup(ServiceLifetime.Transient);

        [GlobalSetup(Target = nameof(Scoped))]
        public void SetupScoped() => Setup(ServiceLifetime.Scoped);

        [GlobalSetup(Target = nameof(Singleton))]
        public void SetupSingleton() => Setup(ServiceLifetime.Singleton);

        [GlobalCleanup]
        public void Cleanup() => ((IDisposable)_serviceProvider).Dispose();

        private void Setup(ServiceLifetime lifetime)
        {
            IServiceCollection services = new ServiceCollection();
            for (int i = 0; i < 10; i++)
            {
                services.Add(ServiceDescriptor.Describe(typeof(A), typeof(A), lifetime));
            }

            services.Add(ServiceDescriptor.Describe(typeof(B), typeof(B), lifetime));
            services.Add(ServiceDescriptor.Describe(typeof(C), typeof(C), lifetime));

            _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions()
            {
#if INTERNAL_DI
                Mode = ServiceProviderMode
#endif
            }).CreateScope().ServiceProvider;
        }

        [Benchmark]
        public object Transient() => _serviceProvider.GetService<IEnumerable<A>>();

        [Benchmark]
        public object Scoped() => _serviceProvider.GetService<IEnumerable<A>>();

        [Benchmark]
        public object Singleton() => _serviceProvider.GetService<IEnumerable<A>>();
    }
}
