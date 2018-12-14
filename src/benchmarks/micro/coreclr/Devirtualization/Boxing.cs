// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

// Performance tests for optimizations related to EqualityComparer<T>.Default

namespace Devirtualization
{
    [BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
    public class Boxing
    {
        [Benchmark]
        public void InterfaceTypeCheckAndCall() => TypeCheckAndCallMethodOnValueTypeInterface(default(Dog));

        private void TypeCheckAndCallMethodOnValueTypeInterface<T>(T thing)
        {
            if (thing is IAnimal)
            {
                ((IAnimal)thing).MakeSound();
            }
        }

        private struct Dog : IAnimal
        {
            public void Bark() { }
            void IAnimal.MakeSound() => Bark();
        }

        private interface IAnimal
        {
            void MakeSound();
        }
    }
}
