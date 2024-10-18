// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

[MemoryDiagnoser]
[BenchmarkCategory(Categories.Runtime)]
public class ArrayDeAbstraction
{
    static readonly int[] s_ro_array;
    static int[] s_array;
    int[] m_array;

    static int[] newArr() => new int[512];

    [GlobalSetup]
    public void Setup()
    {
        m_array = newArr();
    }

    static ArrayDeAbstraction()
    {
        s_ro_array = newArr();
        s_array = newArr();
    }

    static IEnumerable<int> get_static_readonly_array() => s_ro_array;

    static IEnumerable<int> get_static_array() => s_array;

    IEnumerable<int> get_member_array() => m_array;

    [MethodImpl(MethodImplOptions.NoInlining)]
    IEnumerable<int> get_opaque_array() => m_array;

    [Benchmark(Baseline = true)]
    public int foreach_static_readonly_array()
    {
        int sum = 0;
        foreach (int i in s_ro_array) sum += i;
        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public int foreach_static_readonly_array_in_loop()
    {
        int sum = 0;
        for (int j = 0; j < 16; j++)
        {
            foreach (int i in s_ro_array) sum += i;
        }
        return sum;
    }

    [Benchmark]
    public int foreach_static_readonly_array_via_local()
    {
        int[] e = s_ro_array;
        int sum = 0;
        foreach (int i in e) sum += i;
        return sum;
    }

    [Benchmark]
    public int foreach_static_readonly_array_via_interface()
    {
        IEnumerable<int> e = s_ro_array;
        int sum = 0;
        foreach (int i in e) sum += i;
        return sum;
    }

    [Benchmark]
    public int foreach_static_readonly_array_via_interface_property()
    {
        var e = get_static_readonly_array();
        int sum = 0;
        foreach (int i in e) sum += i;
        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public int foreach_static_readonly_array_via_interface_property_in_loop()
    {
        int sum = 0;
        for (int j = 0; j < 16; j++)
        {
            IEnumerable<int> e = get_static_readonly_array();
            foreach (int i in e) sum += i;
        }
        return sum;
    }

    [Benchmark]
    public int foreach_static_array()
    {
        int sum = 0;
        foreach (int i in s_array) sum += i;
        return sum;
    }

    [Benchmark]
    public int foreach_static_array_via_local()
    {
        int[] e = s_array;
        int sum = 0;
        foreach (int i in e) sum += i;
        return sum;
    }

    [Benchmark]
    public int foreach_static_array_via_interface()
    {
        IEnumerable<int> e = s_array;
        int sum = 0;
        foreach (int i in e) sum += i;
        return sum;
    }

    [Benchmark]
    public int foreach_static_array_via_interface_property()
    {
        IEnumerable<int> e = get_static_array();
        int sum = 0;
        foreach (int i in e) sum += i;
        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public int foreach_static_array_via_interface_property_in_loop()
    {
        int sum = 0;
        for (int j = 0; j < 16; j++)
        {
            IEnumerable<int> e = get_static_array();
            foreach (int i in e) sum += i;
        }
        return sum;
    }

    [Benchmark]
    public int foreach_member_array()
    {
        int sum = 0;
        foreach (int i in m_array) sum += i;
        return sum;
    }

    [Benchmark]
    public int foreach_member_array_via_local()
    {
        int[] e = m_array;
        int sum = 0;
        foreach (int i in e) sum += i;
        return sum;
    }

    [Benchmark]
    public int foreach_member_array_via_interface()
    {
        IEnumerable<int> e = m_array;
        int sum = 0;
        foreach (int i in e) sum += i;
        return sum;
    }

    [Benchmark]
    public int foreach_member_array_via_interface_property()
    {
        IEnumerable<int> e = get_member_array();
        int sum = 0;
        foreach (int i in e) sum += i;
        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public int foreach_member_array_via_interface_property_in_loop()
    {
        int sum = 0;
        for (int j = 0; j < 16; j++)
        {
            IEnumerable<int> e = get_member_array();
            foreach (int i in e) sum += i;
        }
        return sum;
    }

    [Benchmark]
    public int foreach_opaque_array_via_interface()
    {
        IEnumerable<int> e = get_opaque_array();
        int sum = 0;
        foreach (int i in e) sum += i;
        return sum;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public int foreach_opaque_array_via_interface_in_loop()
    {
        int sum = 0;
        for (int j = 0; j < 16; j++)
        {
            IEnumerable<int> e = get_opaque_array();
            foreach (int i in e) sum += i;
        }
        return sum;
    }

    [Benchmark]
    public int sum_static_array_via_local()
    {
        int[] a = s_array;
        IEnumerable<int> e = a;
        return e.Sum();
    }
}

