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
        [Benchmark]
        [ArgumentsSource(nameof(DelegateInvokeArguments))]
        public void DelegateInvoke(DelegateLong dl, Object obj) => dl(obj, 100, 100);

        public IEnumerable<object[]> DelegateInvokeArguments()
        {
            yield return new object[] { new DelegateLong(this.Invocable1), new Object() };
        }

        [Benchmark]
        [ArgumentsSource(nameof(MulticastDelegateCombineInvokerAguments))]
        public MultiDelegate MulticastDelegateCombineInvoke(MultiDelegate md1, MultiDelegate md2, MultiDelegate md3, MultiDelegate md4, MultiDelegate md5, 
            MultiDelegate md6, MultiDelegate md7, MultiDelegate md8, MultiDelegate md9, MultiDelegate md10)
        {
            MultiDelegate md = null;
            
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

            return md;
        }
        
        public IEnumerable<object[]> MulticastDelegateCombineInvokerAguments()
        {
            yield return new object[]
            {
                new MultiDelegate(this.Invocable2), 
                new MultiDelegate(this.Invocable2), 
                new MultiDelegate(this.Invocable2), 
                new MultiDelegate(this.Invocable2), 
                new MultiDelegate(this.Invocable2), 
                new MultiDelegate(this.Invocable2), 
                new MultiDelegate(this.Invocable2),
                new MultiDelegate(this.Invocable2), 
                new MultiDelegate(this.Invocable2), 
                new MultiDelegate(this.Invocable2)
            };
        }

        [Benchmark]
        [ArgumentsSource(nameof(MulticastDelegateInvokeArguments))]
        public void MulticastDelegateInvoke(int length, MultiDelegate md, Object obj) => md(obj, 100, 100);
        
        public IEnumerable<object[]> MulticastDelegateInvokeArguments()
        {
            foreach (var length in new[] { 100, 1000 })
            {
                MultiDelegate md = null;
                Object obj = new Object();

                for (long i = 0; i < length; i++)
                    md = (MultiDelegate)Delegate.Combine(new MultiDelegate(this.Invocable2), md);
                
                yield return new object[] { length, md, obj };
            }
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
