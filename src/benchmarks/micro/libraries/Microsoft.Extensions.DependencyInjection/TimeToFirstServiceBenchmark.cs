﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Extensions.DependencyInjection.Performance
{
    public class TimeToFirstServiceBenchmark
    {
        private IServiceProvider _transientSp;
        private IServiceScope _scopedSp;
        private IServiceProvider _singletonSp;
        private ServiceCollection _transientServices;
        private ServiceCollection _scopedServices;
        private ServiceCollection _singletonServices;
        private ServiceProviderMode _mode;

        [Params("Expressions", "Dynamic", "Runtime", "ILEmit")]
        public string Mode {
            set {
                _mode = (ServiceProviderMode)Enum.Parse(typeof(ServiceProviderMode), value);
            }
        }

        [Benchmark(Baseline = true)]
        public void NoDI()
        {
            var temp = new A(new B(new C()));
            temp.Foo();
        }

        [GlobalSetup(Target = nameof(BuildProvider))]
        public void SetupBuildProvider()
        {
            _transientServices = new ServiceCollection();
            _transientServices.AddTransient<A>();
            _transientServices.AddTransient<B>();
            _transientServices.AddTransient<C>();
        }

        [Benchmark]
        public void BuildProvider()
        {
            _transientSp = _transientServices.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = _mode
            });
        }

        [GlobalSetup(Target = nameof(Transient))]
        public void SetupTransient()
        {
            _transientServices = new ServiceCollection();
            _transientServices.AddTransient<A>();
            _transientServices.AddTransient<B>();
            _transientServices.AddTransient<C>();
        }

        [Benchmark]
        public void Transient()
        {
            _transientSp = _transientServices.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = _mode
            });
            var temp = _transientSp.GetService<A>();
            temp.Foo();
        }

        [GlobalSetup(Target = nameof(Scoped))]
        public void SetupScoped()
        {
            _scopedServices = new ServiceCollection();
            _scopedServices.AddScoped<A>();
            _scopedServices.AddScoped<B>();
            _scopedServices.AddScoped<C>();
        }

        [Benchmark]
        public void Scoped()
        {
            _scopedSp = _scopedServices.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = _mode
            }).CreateScope();
            var temp = _scopedSp.ServiceProvider.GetService<A>();
            temp.Foo();
        }

        [GlobalSetup(Target = nameof(Singleton))]
        public void SetupSingleton()
        {
            _singletonServices = new ServiceCollection();
            _singletonServices.AddSingleton<A>();
            _singletonServices.AddSingleton<B>();
            _singletonServices.AddSingleton<C>();
        }

        [Benchmark]
        public void Singleton()
        {
            _singletonSp = _singletonServices.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = _mode
            });
            var temp = _singletonSp.GetService<A>();
            temp.Foo();
        }

        private class A
        {
            public A(B b)
            {

            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        private class B
        {
            public B(C c)
            {

            }
        }

        private class C
        {

        }
    }
}
