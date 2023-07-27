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
        private Type[] _types0;
        private Type[] _types2;
        private Type[] _types2_OutOfOrder;
        private Type[] _types3;

        private ObjectFactory _factory0;
        private ObjectFactory _factory2;
        private ObjectFactory _factory2_OutOfOrder;
        private ObjectFactory _factory3;
        private object[] _factoryArguments0;
        private object[] _factoryArguments1;
        private object[] _factoryArguments2;
        private object[] _factoryArguments2_OutOfOrder;
        private object[] _factoryArguments3;
        private object[] _factoryArguments4;
        private object[] _factoryArguments5;

        [GlobalSetup]
        public void SetUp()
        {
            ServiceCollection collection = new();
            collection.AddTransient<TypeWith0ParametersToBeActivated>();
            collection.AddTransient<TypeWith1ParameterToBeActivated>();
            collection.AddTransient<TypeWith2ParametersToBeActivated>();
            collection.AddTransient<TypeWith3ParametersToBeActivated>();
            collection.AddTransient<TypeWith4ParametersToBeActivated>();
            collection.AddTransient<TypeWith5ParametersToBeActivated>();

            collection.AddSingleton<DependencyA>();
            collection.AddSingleton<DependencyB>();
            collection.AddSingleton<DependencyC>();
            collection.AddSingleton<DependencyD>();
            collection.AddSingleton<DependencyE>();

            _types0 = new Type[] { };
            _types2 = new Type[] { typeof(DependencyA), typeof(DependencyB) };
            _types2_OutOfOrder = new Type[] { typeof(DependencyB), typeof(DependencyC) };
            _types3 = new Type[] { typeof(DependencyA), typeof(DependencyB), typeof(DependencyC) };

            _factory0 = ActivatorUtilities.CreateFactory(typeof(TypeWith3ParametersToBeActivated), _types0);
            _factory2 = ActivatorUtilities.CreateFactory(typeof(TypeWith3ParametersToBeActivated), _types2);
            _factory2_OutOfOrder = ActivatorUtilities.CreateFactory(typeof(TypeWith3ParametersToBeActivated), _types2_OutOfOrder);
            _factory3 = ActivatorUtilities.CreateFactory(typeof(TypeWith3ParametersToBeActivated), _types3);

            _factoryArguments0 = Array.Empty<object>();
            _factoryArguments1 = new object[] { new DependencyA() };
            _factoryArguments2 = new object[] { new DependencyA(), new DependencyB() };
            _factoryArguments2_OutOfOrder = new object[] { new DependencyB(), new DependencyC() };
            _factoryArguments3 = new object[] { new DependencyA(), new DependencyB(), new DependencyC() };
            _factoryArguments4 = new object[] { new DependencyA(), new DependencyB(), new DependencyC(), new DependencyD() };
            _factoryArguments5 = new object[] { new DependencyA(), new DependencyB(), new DependencyC(), new DependencyD(), new DependencyE() };
            _serviceProvider = collection.BuildServiceProvider();
        }

        [GlobalCleanup]
        public void Cleanup() => _serviceProvider.Dispose();

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith0ParametersToBeActivated GetService_0Injected()
        {
            return _serviceProvider.GetService<TypeWith0ParametersToBeActivated>();
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith1ParameterToBeActivated GetService_1Injected()
        {
            return _serviceProvider.GetService<TypeWith1ParameterToBeActivated>();
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith2ParametersToBeActivated GetService_2Injected()
        {
            return _serviceProvider.GetService<TypeWith2ParametersToBeActivated>();
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith3ParametersToBeActivated GetService_3Injected()
        {
            return _serviceProvider.GetService<TypeWith3ParametersToBeActivated>();
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith4ParametersToBeActivated GetService_4Injected()
        {
            return _serviceProvider.GetService<TypeWith4ParametersToBeActivated>();
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith5ParametersToBeActivated GetService_5Injected()
        {
            return _serviceProvider.GetService<TypeWith5ParametersToBeActivated>();
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith0ParametersToBeActivated CreateInstance_0() => ActivatorUtilities.CreateInstance<TypeWith0ParametersToBeActivated>(_serviceProvider, _factoryArguments0);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith1ParameterToBeActivated CreateInstance_1() => ActivatorUtilities.CreateInstance<TypeWith1ParameterToBeActivated>(_serviceProvider, _factoryArguments1);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith2ParametersToBeActivated CreateInstance_2() => ActivatorUtilities.CreateInstance<TypeWith2ParametersToBeActivated>(_serviceProvider, _factoryArguments2);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith3ParametersToBeActivated CreateInstance_3() => ActivatorUtilities.CreateInstance<TypeWith3ParametersToBeActivated>(_serviceProvider, _factoryArguments3);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith4ParametersToBeActivated CreateInstance_4() => ActivatorUtilities.CreateInstance<TypeWith4ParametersToBeActivated>(_serviceProvider, _factoryArguments4);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith5ParametersToBeActivated CreateInstance_5() => ActivatorUtilities.CreateInstance<TypeWith5ParametersToBeActivated>(_serviceProvider, _factoryArguments5);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith3ParametersToBeActivated Factory_1Injected_2Explicit()
        {
            return (TypeWith3ParametersToBeActivated)_factory2(_serviceProvider, _factoryArguments2);
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith3ParametersToBeActivated Factory_1Injected_2Explicit_OutOfOrder()
        {
            return (TypeWith3ParametersToBeActivated)_factory2_OutOfOrder(_serviceProvider, _factoryArguments2_OutOfOrder);
        }
       
        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith3ParametersToBeActivated Factory_3Explicit()
        {
            return (TypeWith3ParametersToBeActivated)_factory3(_serviceProvider, _factoryArguments3);
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith3ParametersToBeActivated Factory_3Injected()
        {
            return (TypeWith3ParametersToBeActivated)_factory0(_serviceProvider, _factoryArguments0);
        }

        public class TypeWith0ParametersToBeActivated
        {
            // DI picks these over the correct one below and throws TargetInvocationException.
            //public TypeWith0ParametersToBeActivated(int i)
            //{
            //    throw new NotImplementedException();
            //}

            //public TypeWith0ParametersToBeActivated(string s)
            //{
            //    throw new NotImplementedException();
            //}

            //public TypeWith0ParametersToBeActivated(object o)
            //{
            //    throw new NotImplementedException();
            //}

            public TypeWith0ParametersToBeActivated()
            {
            }
        }

        public class TypeWith1ParameterToBeActivated
        {
            public DependencyA _a;

            public TypeWith1ParameterToBeActivated(int i)
            {
                throw new NotImplementedException();
            }

            public TypeWith1ParameterToBeActivated(string s)
            {
                throw new NotImplementedException();
            }

            // DI picks this over the one below and calls it.
            //public TypeWith1ParameterToBeActivated(object o)
            //{
            //    throw new NotImplementedException();
            //}

            public TypeWith1ParameterToBeActivated(DependencyA a)
            {
                _a = a;
            }
        }

        public class TypeWith2ParametersToBeActivated
        {
            public DependencyA _a;
            public DependencyB _b;

            public TypeWith2ParametersToBeActivated(int i)
            {
                throw new NotImplementedException();
            }

            public TypeWith2ParametersToBeActivated(string s)
            {
                throw new NotImplementedException();
            }

            public TypeWith2ParametersToBeActivated(object o)
            {
                throw new NotImplementedException();
            }

            public TypeWith2ParametersToBeActivated(DependencyA a, DependencyB b)
            {
                _a = a;
                _b = b;
            }
        }

        public class TypeWith3ParametersToBeActivated
        {
            public DependencyA _a;
            public DependencyB _b;
            public DependencyC _c;

            // DI picks these over the correct one below and throws InvalidOperation.
            //public TypeWith3ParametersToBeActivated(int i)
            //{
            //    throw new NotImplementedException();
            //}

            //public TypeWith3ParametersToBeActivated(string s)
            //{
            //    throw new NotImplementedException();
            //}

            //public TypeWith3ParametersToBeActivated(object o)
            //{
            //    throw new NotImplementedException();
            //}

            public TypeWith3ParametersToBeActivated(DependencyA a, DependencyB b, DependencyC c)
            {
                _a = a;
                _b = b;
                _c = c;
            }
        }

        public class TypeWith4ParametersToBeActivated
        {
            public DependencyA _a;
            public DependencyB _b;
            public DependencyC _c;
            public DependencyD _d;

            public TypeWith4ParametersToBeActivated(int i)
            {
                throw new NotImplementedException();
            }

            public TypeWith4ParametersToBeActivated(string s)
            {
                throw new NotImplementedException();
            }

            public TypeWith4ParametersToBeActivated(object o)
            {
                throw new NotImplementedException();
            }

            public TypeWith4ParametersToBeActivated(DependencyA a, DependencyB b, DependencyC c, DependencyD d)
            {
                _a = a;
                _b = b;
                _c = c;
                _d = d;
            }
        }

        public class TypeWith5ParametersToBeActivated
        {
            public DependencyA _a;
            public DependencyB _b;
            public DependencyC _c;
            public DependencyD _d;
            public DependencyE _e;

            public TypeWith5ParametersToBeActivated(int i)
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated(string s)
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated(object o)
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated(DependencyA a, DependencyB b, DependencyC c, DependencyD d, DependencyE e)
            {
                _a = a;
                _b = b;
                _c = c;
                _d = d;
                _e = e;
            }
        }

        public class DependencyA { }
        public class DependencyB { }
        public class DependencyC { }
        public class DependencyD { }
        public class DependencyE { }
    }
}
