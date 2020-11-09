// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Microsoft.Extensions.DependencyInjection
{
    [BenchmarkCategory(Categories.Libraries)]
    public class ActivatorUtilitiesBenchmark
    {
        private ServiceProvider _serviceProvider;
        private ObjectFactory _factory;
        private object[] _factoryArguments;

        [GlobalSetup]
        public void SetUp()
        {
            var collection = new ServiceCollection();
            collection.AddTransient<TypeToBeActivated>();
            collection.AddSingleton<DependencyA>();
            collection.AddSingleton<DependencyB>();
            collection.AddSingleton<DependencyC>();
            collection.AddTransient<TypeToBeActivated>();

            _serviceProvider = collection.BuildServiceProvider();
            _factory = ActivatorUtilities.CreateFactory(typeof(TypeToBeActivated), new Type[] { typeof(DependencyB), typeof(DependencyC) });
            _factoryArguments = new object[] { new DependencyB(), new DependencyC() };
        }

        [GlobalCleanup]
        public void Cleanup() => _serviceProvider.Dispose();

        [Benchmark]
        public TypeToBeActivated ServiceProvider() => _serviceProvider.GetService<TypeToBeActivated>();

        [Benchmark]
        public TypeToBeActivated Factory() => (TypeToBeActivated)_factory(_serviceProvider, _factoryArguments);

        [Benchmark]
        public TypeToBeActivated CreateInstance() => ActivatorUtilities.CreateInstance<TypeToBeActivated>(_serviceProvider, _factoryArguments);

        public class TypeToBeActivated
        {
            public TypeToBeActivated(int i)
            {
                throw new NotImplementedException();
            }

            public TypeToBeActivated(string s)
            {
                throw new NotImplementedException();
            }

            public TypeToBeActivated(object o)
            {
                throw new NotImplementedException();
            }

            public TypeToBeActivated(DependencyA a, DependencyB b, DependencyC c)
            {
            }
        }

        public class DependencyA {}
        public class DependencyB {}
        public class DependencyC {}
    }
}
