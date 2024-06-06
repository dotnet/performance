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
        private object[] _factoryArguments5;

        [GlobalSetup]
        public void SetUp()
        {
            ServiceCollection collection = new();

            collection.AddSingleton<DependencyA>();
            collection.AddSingleton<DependencyB>();
            collection.AddSingleton<DependencyC>();
            collection.AddSingleton<DependencyD>();
            collection.AddSingleton<DependencyE>();

            _types0 = new Type[] { };
            _types2 = new Type[] { typeof(DependencyA), typeof(DependencyB) };
            _types2_OutOfOrder = new Type[] { typeof(DependencyB), typeof(DependencyC) };
            _types3 = new Type[] { typeof(DependencyA), typeof(DependencyB), typeof(DependencyC) };

            _factory0 = ActivatorUtilities.CreateFactory(typeof(TypeWithVaryingIncomingParametersToBeActivated), _types0);
            _factory2 = ActivatorUtilities.CreateFactory(typeof(TypeWithVaryingIncomingParametersToBeActivated), _types2);
            _factory2_OutOfOrder = ActivatorUtilities.CreateFactory(typeof(TypeWithVaryingIncomingParametersToBeActivated), _types2_OutOfOrder);
            _factory3 = ActivatorUtilities.CreateFactory(typeof(TypeWithVaryingIncomingParametersToBeActivated), _types3);

            _factoryArguments0 = Array.Empty<object>();
            _factoryArguments1 = new object[] { new DependencyA() };
            _factoryArguments2 = new object[] { new DependencyA(), new DependencyB() };
            _factoryArguments2_OutOfOrder = new object[] { new DependencyB(), new DependencyC() };
            _factoryArguments3 = new object[] { new DependencyA(), new DependencyB(), new DependencyC() };
            _factoryArguments5 = new object[] { new DependencyA(), new DependencyB(), new DependencyC(), new DependencyD(), new DependencyE() };
            _serviceProvider = collection.BuildServiceProvider();
        }

        [GlobalCleanup]
        public void Cleanup() => _serviceProvider.Dispose();

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith1ParameterToBeActivated GetService_1Injected()
        {
            return _serviceProvider.GetService<TypeWith1ParameterToBeActivated>();
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith3ParametersToBeActivated GetService_3Injected()
        {
            return _serviceProvider.GetService<TypeWith3ParametersToBeActivated>();
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        [MemoryRandomization]
        public TypeWith5ParametersToBeActivated GetService_5Injected()
        {
            return _serviceProvider.GetService<TypeWith5ParametersToBeActivated>();
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith1ParameterToBeActivated CreateInstance_1() => ActivatorUtilities.CreateInstance<TypeWith1ParameterToBeActivated>(_serviceProvider, _factoryArguments1);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith3ParametersToBeActivated CreateInstance_3() => ActivatorUtilities.CreateInstance<TypeWith3ParametersToBeActivated>(_serviceProvider, _factoryArguments3);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith5ParametersToBeActivated CreateInstance_5() => ActivatorUtilities.CreateInstance<TypeWith5ParametersToBeActivated>(_serviceProvider, _factoryArguments5);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith1ParameterToBeActivated_WithAttrFirst CreateInstance_1_WithAttrFirst() => ActivatorUtilities.CreateInstance<TypeWith1ParameterToBeActivated_WithAttrFirst>(_serviceProvider, _factoryArguments1);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith3ParametersToBeActivated_WithAttrFirst CreateInstance_3_WithAttrFirst() => ActivatorUtilities.CreateInstance<TypeWith3ParametersToBeActivated_WithAttrFirst>(_serviceProvider, _factoryArguments3);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith5ParametersToBeActivated_WithAttrFirst CreateInstance_5_WithAttrFirst() => ActivatorUtilities.CreateInstance<TypeWith5ParametersToBeActivated_WithAttrFirst>(_serviceProvider, _factoryArguments5);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith1ParameterToBeActivated_WithAttrLast CreateInstance_1_WithAttrLast() => ActivatorUtilities.CreateInstance<TypeWith1ParameterToBeActivated_WithAttrLast>(_serviceProvider, _factoryArguments1);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith3ParametersToBeActivated_WithAttrLast CreateInstance_3_WithAttrLast() => ActivatorUtilities.CreateInstance<TypeWith3ParametersToBeActivated_WithAttrLast>(_serviceProvider, _factoryArguments3);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWith5ParametersToBeActivated_WithAttrLast CreateInstance_5_WithAttrLast() => ActivatorUtilities.CreateInstance<TypeWith5ParametersToBeActivated_WithAttrLast>(_serviceProvider, _factoryArguments5);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWithVaryingIncomingParametersToBeActivated Factory_1Injected_2Explicit()
        {
            return (TypeWithVaryingIncomingParametersToBeActivated)_factory2(_serviceProvider, _factoryArguments2);
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWithVaryingIncomingParametersToBeActivated Factory_1Injected_2Explicit_OutOfOrder()
        {
            return (TypeWithVaryingIncomingParametersToBeActivated)_factory2_OutOfOrder(_serviceProvider, _factoryArguments2_OutOfOrder);
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWithVaryingIncomingParametersToBeActivated Factory_3Explicit()
        {
            return (TypeWithVaryingIncomingParametersToBeActivated)_factory3(_serviceProvider, _factoryArguments3);
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public TypeWithVaryingIncomingParametersToBeActivated Factory_3Injected()
        {
            return (TypeWithVaryingIncomingParametersToBeActivated)_factory0(_serviceProvider, _factoryArguments0);
        }

        public class TypeWith1ParameterToBeActivated
        {
            public DependencyA _a;

            public TypeWith1ParameterToBeActivated()
            {
                throw new NotImplementedException();
            }

            public TypeWith1ParameterToBeActivated(DependencyA a)
            {
                _a = a;
            }
        }

        public class TypeWith1ParameterToBeActivated_WithAttrFirst
        {
            public DependencyA _a;

            public TypeWith1ParameterToBeActivated_WithAttrFirst()
            {
                throw new NotImplementedException();
            }

            [ActivatorUtilitiesConstructor]
            public TypeWith1ParameterToBeActivated_WithAttrFirst(DependencyA a)
            {
                _a = a;
            }
        }

        public class TypeWith1ParameterToBeActivated_WithAttrLast
        {
            public DependencyA _a;

            public TypeWith1ParameterToBeActivated_WithAttrLast()
            {
                throw new NotImplementedException();
            }

            [ActivatorUtilitiesConstructor]
            public TypeWith1ParameterToBeActivated_WithAttrLast(DependencyA a)
            {
                _a = a;
            }
        }

        public class TypeWith3ParametersToBeActivated
        {
            public DependencyA _a;
            public DependencyB _b;
            public DependencyC _c;

            public TypeWith3ParametersToBeActivated()
            {
                throw new NotImplementedException();
            }

            public TypeWith3ParametersToBeActivated(int i)
            {
                throw new NotImplementedException();
            }

            public TypeWith3ParametersToBeActivated(int i, int i2)
            {
                throw new NotImplementedException();
            }

            public TypeWith3ParametersToBeActivated(DependencyA a, DependencyB b, DependencyC c)
            {
                _a = a;
                _b = b;
                _c = c;
            }
        }

        public class TypeWith3ParametersToBeActivated_WithAttrFirst
        {
            public DependencyA _a;
            public DependencyB _b;
            public DependencyC _c;

            [ActivatorUtilitiesConstructor]
            public TypeWith3ParametersToBeActivated_WithAttrFirst(DependencyA a, DependencyB b, DependencyC c)
            {
                _a = a;
                _b = b;
                _c = c;
            }

            public TypeWith3ParametersToBeActivated_WithAttrFirst()
            {
                throw new NotImplementedException();
            }

            public TypeWith3ParametersToBeActivated_WithAttrFirst(int i)
            {
                throw new NotImplementedException();
            }

            public TypeWith3ParametersToBeActivated_WithAttrFirst(int i, int i2)
            {
                throw new NotImplementedException();
            }
        }

        public class TypeWith3ParametersToBeActivated_WithAttrLast
        {
            public DependencyA _a;
            public DependencyB _b;
            public DependencyC _c;

            public TypeWith3ParametersToBeActivated_WithAttrLast()
            {
                throw new NotImplementedException();
            }

            public TypeWith3ParametersToBeActivated_WithAttrLast(int i)
            {
                throw new NotImplementedException();
            }

            public TypeWith3ParametersToBeActivated_WithAttrLast(int i, int i2)
            {
                throw new NotImplementedException();
            }

            [ActivatorUtilitiesConstructor]
            public TypeWith3ParametersToBeActivated_WithAttrLast(DependencyA a, DependencyB b, DependencyC c)
            {
                _a = a;
                _b = b;
                _c = c;
            }
        }

        public class TypeWith5ParametersToBeActivated
        {
            public DependencyA _a;
            public DependencyB _b;
            public DependencyC _c;
            public DependencyD _d;
            public DependencyE _e;

            public TypeWith5ParametersToBeActivated()
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated(int i)
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated(int i, int i2)
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated(int i, int i2, int i3)
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated(int i, int i2, int i3, int i4)
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

        public class TypeWith5ParametersToBeActivated_WithAttrFirst
        {
            public DependencyA _a;
            public DependencyB _b;
            public DependencyC _c;
            public DependencyD _d;
            public DependencyE _e;

            [ActivatorUtilitiesConstructor]
            public TypeWith5ParametersToBeActivated_WithAttrFirst(DependencyA a, DependencyB b, DependencyC c, DependencyD d, DependencyE e)
            {
                _a = a;
                _b = b;
                _c = c;
                _d = d;
                _e = e;
            }

            public TypeWith5ParametersToBeActivated_WithAttrFirst()
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated_WithAttrFirst(int i)
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated_WithAttrFirst(int i, int i2)
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated_WithAttrFirst(int i, int i2, int i3)
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated_WithAttrFirst(int i, int i2, int i3, int i4)
            {
                throw new NotImplementedException();
            }
        }

        public class TypeWith5ParametersToBeActivated_WithAttrLast
        {
            public DependencyA _a;
            public DependencyB _b;
            public DependencyC _c;
            public DependencyD _d;
            public DependencyE _e;

            public TypeWith5ParametersToBeActivated_WithAttrLast()
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated_WithAttrLast(int i)
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated_WithAttrLast(int i, int i2)
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated_WithAttrLast(int i, int i2, int i3)
            {
                throw new NotImplementedException();
            }

            public TypeWith5ParametersToBeActivated_WithAttrLast(int i, int i2, int i3, int i4)
            {
                throw new NotImplementedException();
            }

            [ActivatorUtilitiesConstructor]
            public TypeWith5ParametersToBeActivated_WithAttrLast(DependencyA a, DependencyB b, DependencyC c, DependencyD d, DependencyE e)
            {
                _a = a;
                _b = b;
                _c = c;
                _d = d;
                _e = e;
            }
        }

        public class TypeWithVaryingIncomingParametersToBeActivated
        {
            public TypeWithVaryingIncomingParametersToBeActivated(DependencyA a, DependencyB b, DependencyC c) { }
        }

        public class DependencyA { }
        public class DependencyB { }
        public class DependencyC { }
        public class DependencyD { }
        public class DependencyE { }
    }
}
