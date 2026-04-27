// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Define INTERNAL_DI on platforms that support emit. This will measure all of the various engines.
// #define INTERNAL_DI

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Microsoft.Extensions.DependencyInjection
{
    [BenchmarkCategory(Categories.Libraries)]
    public class TimeToFirstService
    {
        private ServiceCollection _transientServices;
        private ServiceCollection _scopedServices;
        private ServiceCollection _singletonServices;
        private ServiceProviderMode _mode = ServiceProviderMode.Default;

#if INTERNAL_DI
        [Params("Default", "Expressions", "Dynamic", "Runtime", "ILEmit")]
#else
        [Params("Default")]
#endif
        public string Mode {
            set {
                _mode = (ServiceProviderMode)Enum.Parse(typeof(ServiceProviderMode), value);
            }
        }

        [GlobalSetup(Targets = new[] { nameof(BuildProvider), nameof(Transient) })]
        public void SetupTransient()
        {
            _transientServices = new ServiceCollection();
            _transientServices.AddTransient<A>();
            _transientServices.AddTransient<B>();
            _transientServices.AddTransient<C>();
        }

        [Benchmark]
        public void BuildProvider()
        {
            using ServiceProvider transientSp = BuildServiceProvider(_transientServices);
        }

        [Benchmark]
        public void Transient()
        {
            using ServiceProvider transientSp = BuildServiceProvider(_transientServices);
            var temp = transientSp.GetService<A>();
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
            using ServiceProvider provider = BuildServiceProvider(_scopedServices);
            IServiceScope scopedSp = provider.CreateScope();
            var temp = scopedSp.ServiceProvider.GetService<A>();
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
            using ServiceProvider singletonSp = BuildServiceProvider(_singletonServices);
            var temp = singletonSp.GetService<A>();
            temp.Foo();
        }

        private class A
        {
            public A(B b) { }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo() { }
        }

        private class B
        {
            public B(C c) { }
        }

        private class C { }

        private ServiceProvider BuildServiceProvider(IServiceCollection services)
        {
            if (_mode == ServiceProviderMode.Default)
            {
                return services.BuildServiceProvider();
            }

            Assembly asm = typeof(ServiceProvider).Assembly;

            ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions());

            return _mode switch
            {
                ServiceProviderMode.Dynamic => CreateInstance("Microsoft.Extensions.DependencyInjection.ServiceLookup.DynamicServiceProviderEngine"),
                ServiceProviderMode.Runtime => CreateInstance("Microsoft.Extensions.DependencyInjection.ServiceLookup.RuntimeServiceProviderEngine"),
                ServiceProviderMode.Expressions => CreateInstance("Microsoft.Extensions.DependencyInjection.ServiceLookup.ExpressionsServiceProviderEngine"),
                ServiceProviderMode.ILEmit => CreateInstance("Microsoft.Extensions.DependencyInjection.ServiceLookup.ILEmitServiceProviderEngine"),
                _ => throw new NotSupportedException()
            };

            ServiceProvider CreateInstance(string engineTypeName)
            {
                // Create the engine
                Type engineType = asm.GetType(engineTypeName);
                if (engineType == null)
                {
                    throw new Exception($"Unable to find {engineType} type.");
                }

                ConstructorInfo serviceProviderEngineCtor = engineType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    binder: null,
                    new Type[] { typeof(ServiceProvider) },
                    modifiers: null);

                object serviceProviderEngine;
                if (serviceProviderEngineCtor != null)
                {
                    serviceProviderEngine = serviceProviderEngineCtor.Invoke(new object[] { provider });
                }
                else
                {
                    // Try parameterless ctor.
                    serviceProviderEngineCtor = engineType.GetConstructor(
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                        binder: null,
                        Type.EmptyTypes,
                        modifiers: null);

                    if (serviceProviderEngineCtor == null)
                    {
                        throw new Exception($"Unable to find ctor for {engineTypeName}.");
                    }

                    serviceProviderEngine = serviceProviderEngineCtor.Invoke(null);
                }

                // Set the provider's engine.
                FieldInfo fi = typeof(ServiceProvider).GetField("_engine", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fi == null)
                {
                    throw new Exception($"Unable to find ServiceProvider._engine field.");
                }

                fi.SetValue(provider, serviceProviderEngine);

                return provider;
            }
        }
    }

    internal enum ServiceProviderMode
    {
        Default,
        Dynamic,
        Runtime,
        Expressions,
        ILEmit
    }
}
