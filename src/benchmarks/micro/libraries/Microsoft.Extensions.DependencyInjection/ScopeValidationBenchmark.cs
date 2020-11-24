// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Microsoft.Extensions.DependencyInjection
{
    [BenchmarkCategory(Categories.Libraries)]
    public class ScopeValidation
    {
        private IServiceProvider _transientSp;
        private IServiceProvider _transientSpScopeValidation;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddTransient<A>();
            services.AddTransient<B>();
            services.AddTransient<C>();
            _transientSp = services.BuildServiceProvider();

            services = new ServiceCollection();
            services.AddTransient<A>();
            services.AddTransient<B>();
            services.AddTransient<C>();
            _transientSpScopeValidation = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            ((IDisposable)_transientSp).Dispose();
            ((IDisposable)_transientSpScopeValidation).Dispose();
        }

        [Benchmark(Baseline = true)]
        public A Transient() => _transientSp.GetService<A>();

        [Benchmark]
        public A TransientWithScopeValidation() => _transientSpScopeValidation.GetService<A>();

        public class A
        {
            public A(B b) { }
        }

        public class B
        {
            public B(C c) { }
        }
        public class C { }
    }
}
