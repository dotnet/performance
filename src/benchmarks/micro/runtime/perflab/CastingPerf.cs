// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace PerfLabTests
{
    public interface IFoo
    {
    }

    public interface IFoo_1
    {
    }

    public interface IFoo_2
    {
    }

    public interface IFoo_3
    {
    }

    public interface IFoo_4
    {
    }

    public interface IFoo_5
    {
    }

    // C# lays the interfaces in reverse order in metadata. So IFoo is the first and IFoo_5 is last
    public class Foo : IFoo_5, IFoo_4, IFoo_3, IFoo_2, IFoo_1, IFoo
    {
        public int m_i;
    }

    public class Foo_1 : Foo
    {
        public int m_j;
    }

    public class Foo_2 : Foo_1
    {
        public int m_k;
    }

    public class Foo_3 : Foo_2
    {
        public int m_l;
    }

    public class Foo_4 : Foo_3
    {
        public int m_m;
    }

    public class Foo_5 : Foo_4
    {
        public int m_n;
    }

    // C# lays the interfaces in reverse order in metadata. So IFoo_1 is the first and IFoo is last
    public class Foo2 : IFoo, IFoo_5, IFoo_4, IFoo_3, IFoo_2, IFoo_1
    {
        public int m_i;
    }

    public struct FooSVT
    {
        public int m_i;
        public int m_j;
    }

    public struct FooORVT
    {
        public Object m_o;
        public Foo m_f;
    }

    public interface IMyInterface1 { }
    public interface IMyInterface2 { }
    public class MyClass1 : IMyInterface1 { }
    public class MyClass2 : IMyInterface2 { }
    public class MyClass4<T> : IMyInterface1 { }

    [BenchmarkCategory(Categories.Runtime, Categories.Perflab)]
    public class CastingPerf
    {
        public const int NUM_ARRAY_ELEMENTS = 100;

        public static int InnerIterationCount = 100000; // do not change the value and keep it public static NOT-readonly, ported "as is" from CoreCLR repo

        public static int[] j;
        public static int[] k;
        public static Foo[] foo;
        public static Foo2[] foo2;
        public static Foo[] n;
        public static Foo_5[] foo_5;
        public static FooSVT[] svt;
        public static FooORVT[] orvt;

        public static Object o;
        public static Object[] o_ar;
        public static Foo[] f;
        public static IFoo[] ifo;
        public static IFoo_5[] if_5;
        public static object myClass1;
        public static object myClass2;
        public static Object myClass4;

        public static Object[] myClass1Arr;
        public static Object[] myClass2Arr;
        public static Object myObj;

        static CastingPerf()
        {
            j = new int[NUM_ARRAY_ELEMENTS];
            for (int i = 0; i < j.Length; i++)
            {
                j[i] = i;
            }
            foo = new Foo[NUM_ARRAY_ELEMENTS];
            for (int i = 0; i < foo.Length; i++)
            {
                foo[i] = new Foo();
            }
            foo2 = new Foo2[NUM_ARRAY_ELEMENTS];
            for (int i = 0; i < foo2.Length; i++)
            {
                foo2[i] = new Foo2();
            }
            n = new Foo[NUM_ARRAY_ELEMENTS];
            foo_5 = new Foo_5[NUM_ARRAY_ELEMENTS];
            for (int i = 0; i < foo_5.Length; i++)
            {
                foo_5[i] = new Foo_5();
            }
            svt = new FooSVT[NUM_ARRAY_ELEMENTS];
            orvt = new FooORVT[NUM_ARRAY_ELEMENTS];
        }

        [Benchmark]
        public void ObjFooIsObj()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                o = foo;
        }

        [Benchmark]
        public void ObjFooIsObj2()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                o_ar = foo;
        }

        [GlobalSetup(Target = nameof(ObjObjIsFoo))]
        public void SetupObjObjIsFoo() => o = foo;
        
        [Benchmark]
        public void ObjObjIsFoo()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                o_ar = (Object[]) o;
        }

        [GlobalSetup(Target = nameof(FooObjIsFoo))]
        public void SetupFooObjIsFoo() => o = foo;
        
        [Benchmark]
        public void FooObjIsFoo()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                f = (Foo[]) o;
        }

        [GlobalSetup(Target = nameof(FooObjIsFoo2))]
        public void SetupFooObjIsFoo2() => o_ar = foo;

        [Benchmark]
        public void FooObjIsFoo2()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                f = (Foo[]) o_ar;
        }

        [Benchmark]
        public void FooObjIsNull()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                o = (Foo[]) n;
        }

        [GlobalSetup(Target = nameof(FooObjIsDescendant))]
        public void SetupFooObjIsDescendant() => o = foo_5;
        
        [Benchmark]
        public void FooObjIsDescendant()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                f = (Foo[]) o;
        }

        [Benchmark]
        public void IFooFooIsIFoo()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                ifo = foo;
        }

        [GlobalSetup(Target = nameof(IFooObjIsIFoo))]
        public void SetupIFooObjIsIFoo() => o = foo;

        [Benchmark]
        public void IFooObjIsIFoo()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                ifo = (IFoo[]) o;
        }

        [GlobalSetup(Target = nameof(IFooObjIsIFooInterAlia))]
        public void SetupIFooObjIsIFooInterAlia() => o = foo2;
        
        [Benchmark]
        public void IFooObjIsIFooInterAlia()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                if_5 = (IFoo_5[]) o;
        }

        [GlobalSetup(Target = nameof(IFooObjIsDescendantOfIFoo))]
        public void SetupIFooObjIsDescendantOfIFoo() => o = foo_5;
        
        [Benchmark]
        public void IFooObjIsDescendantOfIFoo()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                ifo = (IFoo[]) o;
        }

        [Benchmark]
        public void ObjInt()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                o = (Object) j;
        }

        [GlobalSetup(Target = nameof(IntObj))]
        public void SetupIntObj() => o = (Object)j;

        [Benchmark]
        public void IntObj()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                k = (int[]) o;
        }

        [Benchmark]
        public void ObjScalarValueType()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                o = svt;
        }
        
        [GlobalSetup(Target = nameof(ScalarValueTypeObj))]
        public void SetupScalarValueTypeObj() => o = svt;
        
        [Benchmark]
        public void ScalarValueTypeObj()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                svt = (FooSVT[]) o;
        }

        [Benchmark]
        public void ObjObjrefValueType()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                o = (Object) orvt;
        }

        [GlobalSetup(Target = nameof(ObjrefValueTypeObj))]
        public void SetupObjrefValueTypeObj() => o = (Object)orvt;
        
        [Benchmark]
        public void ObjrefValueTypeObj()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                orvt = (FooORVT[]) o;
        }
        
        [GlobalSetup(Target = nameof(FooObjCastIfIsa))]
        public void SetupFooObjCastIfIsa() => o = foo;

        [Benchmark]
        public void FooObjCastIfIsa()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                if (o is Foo[])
                    f = (Foo[]) o;
        }

        [GlobalSetup(Target = nameof(CheckObjIsInterfaceYes))]
        public void SetupCheckObjIsInterfaceYes() => myClass1 = new MyClass1();
        
        [Benchmark]
        public bool CheckObjIsInterfaceYes()
        {
            bool res = false;
            for (int i = 0; i < InnerIterationCount; i++)
                res = myClass1 is IMyInterface1;
            return res;
        }

        [GlobalSetup(Target = nameof(CheckObjIsInterfaceNo))]
        public void SetupCheckObjIsInterfaceNo() => myClass2 = new MyClass2();
        
        [Benchmark]
        public bool CheckObjIsInterfaceNo()
        {
            bool res = false;
            for (int i = 0; i < InnerIterationCount; i++)
                res = myClass2 is IMyInterface1;
            return res;
        }

        [GlobalSetup(Target = nameof(CheckIsInstAnyIsInterfaceYes))]
        public void SetupCheckIsInstAnyIsInterfaceYes() => myClass4 = new MyClass4<List<string>>();
        
        [Benchmark]
        public bool CheckIsInstAnyIsInterfaceYes()
        {
            bool res = false;
            for (int i = 0; i < InnerIterationCount; i++)
                res = myClass4 is IMyInterface1;
            return res;
        }

        [GlobalSetup(Target = nameof(CheckIsInstAnyIsInterfaceNo))]
        public void SetupCheckIsInstAnyIsInterfaceNo() => myClass4 = new MyClass4<List<string>>();
        
        [Benchmark]
        public bool CheckIsInstAnyIsInterfaceNo()
        {
            bool res = false;
            for (int i = 0; i < InnerIterationCount; i++)
                res = myClass4 is IMyInterface2;
            return res;
        }

        [GlobalSetup(Target = nameof(CheckArrayIsInterfaceYes))]
        public void SetupCheckArrayIsInterfaceYes() => myClass1Arr = new MyClass1[5];
        
        [Benchmark]
        public bool CheckArrayIsInterfaceYes()
        {
            bool res = false;
            for (int i = 0; i < InnerIterationCount; i++)
                res = myClass1Arr is IMyInterface1[];
            return res;
        }

        [GlobalSetup(Target = nameof(CheckArrayIsInterfaceNo))]
        public void SetupCheckArrayIsInterfaceNo() => myClass2Arr = new MyClass2[5];
        
        [Benchmark]
        public bool CheckArrayIsInterfaceNo()
        {
            bool res = false;
            for (int i = 0; i < InnerIterationCount; i++)
                res = myClass2Arr is IMyInterface1[];
            return res;
        }

        [GlobalSetup(Targets = new[] { 
            nameof(CheckArrayIsNonvariantGenericInterface), 
            nameof(CheckArrayIsNonvariantGenericInterfaceNo),
            nameof(CheckArrayIsArrayByVariance),
            nameof(CheckListIsVariantGenericInterface),
            nameof(CheckArrayIsVariantGenericInterfaceNo)})]
        public void SetupMyObj() => myObj = new MyClass2[5];

        [Benchmark]
        [MemoryRandomization]
        public bool CheckArrayIsNonvariantGenericInterface()
        {
            return myObj is ICollection<MyClass2>;
        }

        [Benchmark]
        [MemoryRandomization]
        public bool CheckArrayIsNonvariantGenericInterfaceNo()
        {
            return myObj is ICollection<Exception>;
        }

        [Benchmark]
        [MemoryRandomization]
        public bool CheckArrayIsArrayByVariance()
        {
            return myObj is IMyInterface2[];
        }

        [Benchmark]
        [MemoryRandomization]
        public bool CheckListIsVariantGenericInterface()
        {
            return myObj is IReadOnlyCollection<object>;
        }

        [Benchmark]
        [MemoryRandomization]
        public bool CheckArrayIsVariantGenericInterfaceNo()
        {
            return myObj is IReadOnlyCollection<Exception>;
        }

        [GlobalSetup(Target = nameof(AssignArrayElementByVariance))]
        public void SetupAssignArrayElementByVariance() => myClass2Arr = new IReadOnlyCollection<object>[5];

        [Benchmark]
        public void AssignArrayElementByVariance()
        {
            // no return is needed. The cast is potentially throwing and thus is not optimizable.
            myClass2Arr[0] = myClass2Arr;
        }

        [Benchmark]
        public bool CheckArrayIsVariantGenericInterfaceReflection()
        {
            return typeof(IReadOnlyCollection<object>).IsAssignableFrom(typeof(MyClass2[]));
        }
    }
}