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
    public class GetService :  ServiceProviderEngineBenchmark
    {
        private IServiceProvider _transientSp;
        private IServiceScope _scopedSp;
        private IServiceProvider _singletonSp;
        private IServiceProvider _serviceScopeFactoryProvider;
        private IServiceProvider _serviceScope;
        private IServiceProvider _emptyEnumerable;

        [Benchmark(Baseline = true)]
        public A NoDI() => new A(new B(new C()));

        [GlobalSetup(Target = nameof(Transient))]
        public void SetupTransient()
        {
            var services = new ServiceCollection();
            services.AddTransient<A>();
            services.AddTransient<B>();
            services.AddTransient<C>();
            _transientSp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
#if INTERNAL_DI
                Mode = ServiceProviderMode
#endif
            });
        }

        [Benchmark]
        public A Transient() => _transientSp.GetService<A>();

        [GlobalSetup(Target = nameof(Scoped))]
        public void SetupScoped()
        {
            var services = new ServiceCollection();
            services.AddScoped<A>();
            services.AddScoped<B>();
            services.AddScoped<C>();
            _scopedSp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
#if INTERNAL_DI
                Mode = ServiceProviderMode
#endif
            }).CreateScope();
        }

        [Benchmark]
        public A Scoped() => _scopedSp.ServiceProvider.GetService<A>();

        [GlobalSetup(Target = nameof(Singleton))]
        public void SetupScopedSingleton()
        {
            var services = new ServiceCollection();
            services.AddSingleton<A>();
            services.AddSingleton<B>();
            services.AddSingleton<C>();
            _singletonSp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
#if INTERNAL_DI
                Mode = ServiceProviderMode
#endif
            });
        }

        [Benchmark]
        public A Singleton() => _singletonSp.GetService<A>();

        [GlobalSetup(Target = nameof(ServiceScope))]
        public void ServiceScopeSetup()
        {
            _serviceScope = new ServiceCollection().BuildServiceProvider(new ServiceProviderOptions()
            {
#if INTERNAL_DI
                Mode = ServiceProviderMode
#endif
            });
        }

        [Benchmark]
        public IServiceScope ServiceScope() => _serviceScope.CreateScope();

        [GlobalSetup(Target = nameof(ServiceScopeProvider))]
        public void ServiceScopeProviderSetup()
        {
            _serviceScopeFactoryProvider = new ServiceCollection().BuildServiceProvider(new ServiceProviderOptions()
            {
#if INTERNAL_DI
                Mode = ServiceProviderMode
#endif
            });
        }

        [Benchmark]
        public IServiceScopeFactory ServiceScopeProvider() => _serviceScopeFactoryProvider.GetService<IServiceScopeFactory>();

        [GlobalSetup(Target = nameof(EmptyEnumerable))]
        public void EmptyEnumerableSetup()
        {
            _emptyEnumerable = new ServiceCollection().BuildServiceProvider(new ServiceProviderOptions()
            {
#if INTERNAL_DI
                Mode = ServiceProviderMode
#endif
            });
        }

        [Benchmark]
        public object EmptyEnumerable() =>_emptyEnumerable.GetService<IEnumerable<A>>();
    }
}
