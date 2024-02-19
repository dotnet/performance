// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Microsoft.Extensions.DependencyInjection
{
    [BenchmarkCategory(Categories.Libraries)]
    public class ScopeValidationOnBuild
    {
        private ServiceCollection _services;

        [GlobalSetup]
        public void Setup()
        {
            _services = new ServiceCollection();
            
            _services.AddTransient<A>();
            _services.AddTransient<B>();
            _services.AddTransient<C>();
            _services.AddTransient<D>();
            _services.AddTransient<E>();
            _services.AddTransient<F>();
            _services.AddTransient<G>();
            _services.AddTransient<H>();
            _services.AddTransient<I>();
            _services.AddTransient<J>();
            _services.AddTransient<K>();
            _services.AddTransient<L>();
            _services.AddTransient<M>();
            _services.AddTransient<N>();
            _services.AddTransient<O>();
            _services.AddTransient<P>();
        }

        [Benchmark]
        public void ValidateOnBuild()
        {
            _services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        }
        
        private class A
        {
            public A(B b, C c, D d, E e, F f, G g, H h, I i, J j, K k, L l)
            {

            }
        }

        private class B
        {
            public B(C c, D d, E e, F f, G g, H h, I i, J j, K k, L l)
            {

            }
        }

        private class C
        {
            public C(D d, E e, F f, G g, H h, I i, J j, K k, L l)
            {

            }

        }

        private class D
        {
            public D(E e, F f, G g, H h, I i, J j, K k, L l)
            {

            }
        }

        private class E
        {
            public E(F f, G g, H h, I i, J j, K k, L l)
            {

            }
        }

        private class F
        {
            public F(G g, H h, I i, J j, K k, L l)
            {

            }
        }

        private class G
        {
            public G(H h, I i, J j, K k, L l)
            {

            }
        }

        private class H
        {
            public H(I i, J j, K k, L l)
            {

            }
        }

        private class I
        {
            public I(J j, K k, L l)
            {

            }
        }

        private class J
        {
            public J(K k, L l)
            {

            }
        }

        private class K
        {
            public K(L l)
            {

            }
        }

        private class L
        {
            public L(M m)
            {

            }
        }

        private class M
        {
            public M(N n)
            {

            }
        }

        private class N
        {
            public N(O o)
            {

            }
        }

        private class O
        {
            public O(P p)
            {

            }
        }

        private class P
        {
            public P()
            {

            }
        }
    }
}