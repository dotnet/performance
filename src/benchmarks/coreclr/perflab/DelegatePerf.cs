// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace PerfLabTests
{
    public delegate long DelegateLong(Object obj, long x, long y);
    public delegate void MultiDelegate(Object obj, long x, long y);

    internal delegate int SerializeDelegate();

    [BenchmarkCategory(Categories.CoreCLR, Categories.Perflab)]
    public class DelegatePerf
    {
        public static int InnerIterationCount200000 = 200000; // do not change the value and keep it public static NOT-readonly, ported "as is" from CoreCLR repo
        public static int InnerIterationCount1000 = 1000; // do not change the value and keep it public static NOT-readonly, ported "as is" from CoreCLR repo
        public static int InnerIterationCount10000 = 10000; // do not change the value and keep it public static NOT-readonly, ported "as is" from CoreCLR repo
        
        DelegateLong dlField;
        Object objField;
        MultiDelegate md1Field, md2Field, md3Field, md4Field, md5Field, md6Field, md7Field, md8Field, md9Field, md10Field;
        MultiDelegate md100Field, md1000Field;
        
        [GlobalSetup(Target = nameof(DelegateInvoke))]
        public void SetupDelegateInvoke()
        {
            dlField = new DelegateLong(this.Invocable1);
            objField = new Object();
        }
        
        [Benchmark]
        public long DelegateInvoke()
        {
            DelegateLong dl = dlField;
            Object obj = objField;

            long ret = 0;

            for (int i = 0; i < InnerIterationCount200000; i++)
                ret = dl(obj, 100, 100);

            return ret;
        }
        
        [IterationSetup(Target = nameof(MulticastDelegateCombineInvoke))]
        public void SetupMulticastDelegateCombineInvoke()
        {
            md1Field = new MultiDelegate(this.Invocable2);
            md2Field = new MultiDelegate(this.Invocable2);
            md3Field = new MultiDelegate(this.Invocable2);
            md4Field = new MultiDelegate(this.Invocable2);
            md5Field = new MultiDelegate(this.Invocable2);
            md6Field = new MultiDelegate(this.Invocable2);
            md7Field = new MultiDelegate(this.Invocable2);
            md8Field = new MultiDelegate(this.Invocable2);
            md9Field = new MultiDelegate(this.Invocable2);
            md10Field = new MultiDelegate(this.Invocable2);
        }

        [Benchmark]
        public MultiDelegate MulticastDelegateCombineInvoke()
        {
            MultiDelegate md1 =  md1Field;
            MultiDelegate md2 =  md2Field;
            MultiDelegate md3 =  md3Field;
            MultiDelegate md4 =  md4Field;
            MultiDelegate md5 =  md5Field;
            MultiDelegate md6 =  md6Field;
            MultiDelegate md7 =  md7Field;
            MultiDelegate md8 =  md8Field;
            MultiDelegate md9 =  md9Field;
            MultiDelegate md10 = md10Field;
            
            MultiDelegate md = null;

            for (int i = 0; i < InnerIterationCount1000; i++)
            {
                md = (MultiDelegate)Delegate.Combine(md1, md);
                md = (MultiDelegate)Delegate.Combine(md2, md);
                md = (MultiDelegate)Delegate.Combine(md3, md);
                md = (MultiDelegate)Delegate.Combine(md4, md);
                md = (MultiDelegate)Delegate.Combine(md5, md);
                md = (MultiDelegate)Delegate.Combine(md6, md);
                md = (MultiDelegate)Delegate.Combine(md7, md);
                md = (MultiDelegate)Delegate.Combine(md8, md);
                md = (MultiDelegate)Delegate.Combine(md9, md);
                md = (MultiDelegate)Delegate.Combine(md10, md);
            }

            return md;
        }

        [GlobalSetup(Target = nameof(MulticastDelegateInvoke))]
        public void SetupMulticastDelegateInvoke()
        {
            objField = new Object();
            
            for (long i = 0; i < 100; i++)
                md100Field = (MultiDelegate)Delegate.Combine(new MultiDelegate(this.Invocable2), md100Field);
            
            for (long i = 0; i < 1000; i++)
                md1000Field = (MultiDelegate)Delegate.Combine(new MultiDelegate(this.Invocable2), md1000Field);
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        public void MulticastDelegateInvoke(int length)
        {
            MultiDelegate md = length == 100 ? md100Field : md1000Field;
            Object obj = objField;

            for (int i = 0; i < InnerIterationCount10000; i++)
                md(obj, 100, 100);
        }

        internal virtual long Invocable1(Object obj, long x, long y)
        {
            long i = x + y;
            return x;
        }

        internal virtual void Invocable2(Object obj, long x, long y)
        {
            long i = x + y;
        }
    }
}
