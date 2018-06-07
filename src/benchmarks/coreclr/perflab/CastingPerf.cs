// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Benchmarks;

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

    [BenchmarkCategory(Categories.CoreCLR, Categories.Perflab)]
    public class CastingPerf
    {
        public const int NUM_ARRAY_ELEMENTS = 100;

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
        public void ObjFooIsObj() => o = foo;

        [Benchmark]
        public void ObjFooIsObj2() => o_ar = foo;

        [GlobalSetup(Target = nameof(ObjObjIsFoo))]
        public void SetupObjObjIsFoo() => o = foo;
        
        [Benchmark]
        public void ObjObjIsFoo() => o_ar = (Object[])o;

        [GlobalSetup(Target = nameof(FooObjIsFoo))]
        public void SetupFooObjIsFoo() => o = foo;
        
        [Benchmark]
        public void FooObjIsFoo() => f = (Foo[])o;

        [GlobalSetup(Target = nameof(FooObjIsFoo2))]
        public void SetupFooObjIsFoo2() => o_ar = foo;
        
        [Benchmark]
        public void FooObjIsFoo2() => f = (Foo[])o_ar;

        [Benchmark]
        public void FooObjIsNull() => o = (Foo[])n;

        [GlobalSetup(Target = nameof(FooObjIsDescendant))]
        public void SetupFooObjIsDescendant() => o = foo_5;
        
        [Benchmark]
        public void FooObjIsDescendant() => f = (Foo[])o;

        [Benchmark]
        public void IFooFooIsIFoo() => ifo = foo;
        
        [GlobalSetup(Target = nameof(IFooObjIsIFoo))]
        public void SetupIFooObjIsIFoo() => o = foo;

        [Benchmark]
        public void IFooObjIsIFoo() => ifo = (IFoo[])o;

        [GlobalSetup(Target = nameof(IFooObjIsIFooInterAlia))]
        public void SetupIFooObjIsIFooInterAlia() => o = foo2;
        
        [Benchmark]
        public void IFooObjIsIFooInterAlia() => if_5 = (IFoo_5[])o;

        [GlobalSetup(Target = nameof(IFooObjIsDescendantOfIFoo))]
        public void SetupIFooObjIsDescendantOfIFoo() => o = foo_5;
        
        [Benchmark]
        public void IFooObjIsDescendantOfIFoo() => ifo = (IFoo[])o;

        [Benchmark]
        public void ObjInt() => o = (Object)j;

        [GlobalSetup(Target = nameof(IntObj))]
        public void SetupIntObj() => o = (Object)j;
        
        [Benchmark]
        public void IntObj() => k = (int[])o;

        [Benchmark]
        public void ObjScalarValueType() => o = svt;
        
        [GlobalSetup(Target = nameof(ScalarValueTypeObj))]
        public void SetupScalarValueTypeObj() => o = svt;

        [Benchmark]
        public void ScalarValueTypeObj() => svt = (FooSVT[])o;

        [Benchmark]
        public void ObjObjrefValueType() => o = (Object)orvt;

        [GlobalSetup(Target = nameof(ObjrefValueTypeObj))]
        public void SetupObjrefValueTypeObj() => o = (Object)orvt;
        
        [Benchmark]
        public void ObjrefValueTypeObj() => orvt = (FooORVT[])o;
        
        [GlobalSetup(Target = nameof(FooObjCastIfIsa))]
        public void SetupFooObjCastIfIsa() => o = foo;

        [Benchmark]
        public void FooObjCastIfIsa()
        {
            if (o is Foo[])
                f = (Foo[])o;
        }

        [GlobalSetup(Target = nameof(CheckObjIsInterfaceYes))]
        public void SetupCheckObjIsInterfaceYes() => myClass1 = new MyClass1();
        
        [Benchmark]
        public bool CheckObjIsInterfaceYes() => myClass1 is IMyInterface1;

        [GlobalSetup(Target = nameof(CheckObjIsInterfaceNo))]
        public void SetupCheckObjIsInterfaceNo() => myClass2 = new MyClass2();
        
        [Benchmark]
        public bool CheckObjIsInterfaceNo() => myClass2 is IMyInterface1;

        [GlobalSetup(Target = nameof(CheckIsInstAnyIsInterfaceYes))]
        public void SetupCheckIsInstAnyIsInterfaceYes() => myClass4 = new MyClass4<List<string>>();
        
        [Benchmark]
        public bool CheckIsInstAnyIsInterfaceYes() => myClass4 is IMyInterface1;

        [GlobalSetup(Target = nameof(CheckIsInstAnyIsInterfaceNo))]
        public void SetupCheckIsInstAnyIsInterfaceNo() => myClass4 = new MyClass4<List<string>>();
        
        [Benchmark]
        public bool CheckIsInstAnyIsInterfaceNo() => myClass4 is IMyInterface2;

        [GlobalSetup(Target = nameof(CheckArrayIsInterfaceYes))]
        public void SetupCheckArrayIsInterfaceYes() => myClass1Arr = new MyClass1[5];
        
        [Benchmark]
        public bool CheckArrayIsInterfaceYes() => myClass1Arr is IMyInterface1[];

        [GlobalSetup(Target = nameof(CheckArrayIsInterfaceNo))]
        public void SetupCheckArrayIsInterfaceNo() => myClass2Arr = new MyClass2[5];
        
        [Benchmark]
        public bool CheckArrayIsInterfaceNo() => myClass2Arr is IMyInterface1[];
    }
}