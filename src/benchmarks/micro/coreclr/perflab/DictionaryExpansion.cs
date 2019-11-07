// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace PerfLabTests
{
    public class GenClass<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type FuncOnGenClass(int level)
        {
            switch (level)
            {
                case 0: return typeof(T);
                case 1: return typeof(List<T>);
                case 2: return typeof(List<List<T>>);
                case 3: return typeof(List<List<List<T>>>);
                case 4: return typeof(List<List<List<List<T>>>>);
                case 5: return typeof(List<List<List<List<List<T>>>>>);
                case 6: return typeof(List<List<List<List<List<List<T>>>>>>);
                case 7: return typeof(List<List<List<List<List<List<List<T>>>>>>>);
                case 8: return typeof(List<List<List<List<List<List<List<List<T>>>>>>>>);
                case 9: return typeof(List<List<List<List<List<List<List<List<List<T>>>>>>>>>);
                case 10: return typeof(List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>);
                case 11: return typeof(List<List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>>);
                case 12: return typeof(List<List<List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>>>);
                default: return typeof(List<List<List<List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>>>>);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type FuncOnGenClass2(int level)
        {
            switch (level)
            {
                case 0: return typeof(T);
                case 1: return typeof(List<T>);
                case 2: return typeof(List<List<T>>);
                case 3: return typeof(List<List<List<T>>>);
                case 4: return typeof(List<List<List<List<T>>>>);
                case 5: return typeof(List<List<List<List<List<T>>>>>);
                case 6: return typeof(List<List<List<List<List<List<T>>>>>>);
                case 7: return typeof(List<List<List<List<List<List<List<T>>>>>>>);
                case 8: return typeof(List<List<List<List<List<List<List<List<T>>>>>>>>);
                case 9: return typeof(List<List<List<List<List<List<List<List<List<T>>>>>>>>>);
                case 10: return typeof(List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>);
                case 11: return typeof(List<List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>>);
                case 12: return typeof(List<List<List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>>>);
                default: return typeof(List<List<List<List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>>>>);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void FuncOnGenClassTest()
        {
            var o1 = new GenClass<string>();
            var o2 = new GenClass<object>();
            var o3 = new GenClass<DictionaryExpansion>();

            for (int i = 0; i < 15; i++)
                o1.FuncOnGenClass(i);

            for (int i = 0; i < 15; i++)
                o2.FuncOnGenClass(i);

            for (int i = 0; i < 15; i++)
                o2.FuncOnGenClass2(i);

            for (int i = 0; i < 15; i++)
                o3.FuncOnGenClass(i);
        }
    }

    [BenchmarkCategory(Categories.CoreCLR, Categories.Perflab)]
    public class DictionaryExpansion
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type GenFunc<T>(int level)
        {
            switch (level)
            {
                case 0: return typeof(T);
                case 1: return typeof(List<T>);
                case 2: return typeof(List<List<T>>);
                case 3: return typeof(List<List<List<T>>>);
                case 4: return typeof(List<List<List<List<T>>>>);
                case 5: return typeof(List<List<List<List<List<T>>>>>);
                case 6: return typeof(List<List<List<List<List<List<T>>>>>>);
                case 7: return typeof(List<List<List<List<List<List<List<T>>>>>>>);
                case 8: return typeof(List<List<List<List<List<List<List<List<T>>>>>>>>);
                case 9: return typeof(List<List<List<List<List<List<List<List<List<T>>>>>>>>>);
                case 10: return typeof(List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>);
                case 11: return typeof(List<List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>>);
                case 12: return typeof(List<List<List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>>>);
                default: return typeof(List<List<List<List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>>>>);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type GenFunc2<T>(int level)
        {
            switch (level)
            {
                case 0: return typeof(T);
                case 1: return typeof(List<T>);
                case 2: return typeof(List<List<T>>);
                case 3: return typeof(List<List<List<T>>>);
                case 4: return typeof(List<List<List<List<T>>>>);
                case 5: return typeof(List<List<List<List<List<T>>>>>);
                case 6: return typeof(List<List<List<List<List<List<T>>>>>>);
                case 7: return typeof(List<List<List<List<List<List<List<T>>>>>>>);
                case 8: return typeof(List<List<List<List<List<List<List<List<T>>>>>>>>);
                case 9: return typeof(List<List<List<List<List<List<List<List<List<T>>>>>>>>>);
                case 10: return typeof(List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>);
                case 11: return typeof(List<List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>>);
                case 12: return typeof(List<List<List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>>>);
                default: return typeof(List<List<List<List<List<List<List<List<List<List<List<List<List<T>>>>>>>>>>>>>);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void GenFuncTest()
        {
            for (int i = 0; i < 15; i++)
                GenFunc<string>(i);

            for (int i = 0; i < 15; i++)
                GenFunc<object>(i);

            for (int i = 0; i < 15; i++)
                GenFunc2<object>(i);

            for (int i = 0; i < 15; i++)
                GenFunc<DictionaryExpansion>(i);
        }

        public static int s_Iterations = 100000;


        //
        // This benchmark is used to measure the performance of generic dictionary lookups.
        // 
        [Benchmark]
        public void ExpandDictionaries()
        {
            GenClass<int>.FuncOnGenClassTest();

            GenFuncTest();
        }
    }
}
