// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MicroBenchmarks;

namespace PerfLabTests.CastingPerf2
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

    [BenchmarkCategory(Categories.Runtime, Categories.Perflab)]
    public class CastingPerf
    {
        public static int InnerIterationCount200000 = 200000; // do not change the value and keep it public static NOT-readonly, ported "as is" from CoreCLR repo
        public static int InnerIterationCount100000 = 100000; // do not change the value and keep it public static NOT-readonly, ported "as is" from CoreCLR repo
        public static  int InnerIterationCount300000 = 300000; // do not change the value and keep it public static NOT-readonly, ported "as is" from CoreCLR repo
        
        public static int j, j1, j2, j3, j4, j5, j6, j7, j8, j9;
        public static Foo foo = new Foo();
        public static Foo2 foo2 = new Foo2();
        public static Foo n = null;
        public static Foo_5 foo_5 = new Foo_5();
        public static FooSVT svt = new FooSVT();
        public static FooORVT orvt = new FooORVT();

        public static Object o, o1, o2, o3, o4, o5, o6, o7, o8, o9;
        public static Foo f, f1, f2, f3, f4, f5, f6, f7, f8, f9;
        public static IFoo ifo, ifo1, ifo2, ifo3, ifo4, ifo5, ifo6, ifo7, ifo8, ifo9;
        public static IFoo_5 if_0, if_1, if_2, if_3, if_4, if_5, if_6, if_7, if_8, if_9;

        [Benchmark]
        public void ObjFooIsObj()
        {
            for (int i = 0; i < InnerIterationCount200000; i++)
                o = foo;
        }

        [GlobalSetup(Target = nameof(FooObjIsFoo))]
        public void SetupFooObjIsFoo() => o = foo;
        
        [Benchmark]
        public void FooObjIsFoo()
        {
            for (int i = 0; i < InnerIterationCount200000; i++)
                f = (Foo)o;
        }
        
        [Benchmark]
        public void FooObjIsNull()
        {
            for (int i = 0; i < InnerIterationCount200000; i++)
                o = (Foo)n;
        }

        [GlobalSetup(Target = nameof(FooObjIsDescendant))]
        public void SetupFooObjIsDescendant() => o = foo_5;
        
        [Benchmark]
        public void FooObjIsDescendant()
        {
            for (int i = 0; i < InnerIterationCount100000; i++)
                f = (Foo)o;
        }

        [Benchmark]
        public void IFooFooIsIFoo()
        {
            for (int i = 0; i < InnerIterationCount200000; i++)
                ifo = foo;
        }

        [GlobalSetup(Target = nameof(IFooObjIsIFoo))]
        public void SetupIFooObjIsIFoo() => o = foo;

        [Benchmark]
        [MemoryRandomization]
        public void IFooObjIsIFoo()
        {
            for (int i = 0; i < InnerIterationCount100000; i++)
                ifo = (IFoo)o;
        }
        
        [GlobalSetup(Target = nameof(IFooObjIsIFooInterAlia))]
        public void SetupIFooObjIsIFooInterAlia() => o = foo2;

        [Benchmark]
        public void IFooObjIsIFooInterAlia()
        {
            for (int i = 0; i < InnerIterationCount100000; i++)
                if_0 = (IFoo_5)o;
        }

        [GlobalSetup(Target = nameof(IFooObjIsDescendantOfIFoo))]
        public void SetupIFooObjIsDescendantOfIFoo() => o = foo_5;
        
        [Benchmark]
        public void IFooObjIsDescendantOfIFoo()
        {
            for (int i = 0; i < InnerIterationCount100000; i++)
                ifo = (IFoo)o;
        }

        [Benchmark]
        public void ObjInt()
        {
            for (int i = 0; i < InnerIterationCount100000; i++)
                o = (Object)j;
        }
        
        [GlobalSetup(Target = nameof(IntObj))]
        public void SetupIntObj() => o = (Object)1;

        [Benchmark]
        public void IntObj()
        {
            for (int i = 0; i < InnerIterationCount200000; i++)
                j = (int)o;
        }

        [Benchmark]
        public void ObjScalarValueType()
        {
            for (int i = 0; i < InnerIterationCount100000; i++)
                o = svt;
        }
        
        [GlobalSetup(Target = nameof(ScalarValueTypeObj))]
        public void SetupScalarValueTypeObj() => o = svt;

        [Benchmark]
        public void ScalarValueTypeObj()
        {
            for (int i = 0; i < InnerIterationCount300000; i++)
                svt = (FooSVT)o;
        }

        [Benchmark]
        public void ObjObjrefValueType()
        {
            for (int i = 0; i < InnerIterationCount100000; i++)
                o = (Object)orvt;
        }

        [GlobalSetup(Target = nameof(ObjrefValueTypeObj))]
        public void SetupObjrefValueTypeObj() => o = (Object)orvt;
        
        [Benchmark]
        public void ObjrefValueTypeObj()
        {
            for (int i = 0; i < InnerIterationCount200000; i++)
                orvt = (FooORVT)o;
        }
        
        [GlobalSetup(Target = nameof(FooObjCastIfIsa))]
        public void StupFooObjCastIfIsa() => o = foo;

        [Benchmark]
        public void FooObjCastIfIsa()
        {
            for (int i = 0; i < InnerIterationCount100000; i++)
                if (o is Foo)
                    f = (Foo)o;
        }
    }
}