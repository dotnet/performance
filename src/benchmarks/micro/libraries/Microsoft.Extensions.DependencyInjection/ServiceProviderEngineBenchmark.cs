// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Microsoft.Extensions.DependencyInjection
{
    [BenchmarkCategory(Categories.Libraries)]
    public class ServiceProviderEngineBenchmark
    {
#if INTERNAL_DI
        internal ServiceProviderMode ServiceProviderMode { get; private set; }

        [Params("Expressions", "Dynamic", "Runtime", "ILEmit")]
        public string Mode {
            set {
                ServiceProviderMode = (ServiceProviderMode)Enum.Parse(typeof(ServiceProviderMode), value);
            }
        }
#endif

        public class A
        {
            public A(B b) { }
        }

        public class B
        {
            public B(C c) { }
        }

        public class C { }
    }
}
