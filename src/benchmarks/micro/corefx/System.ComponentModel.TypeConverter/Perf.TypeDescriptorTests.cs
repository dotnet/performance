// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.ComponentModel.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_TypeDescriptorTests
    {
        [Benchmark]
        [Arguments(typeof(int))] // primitive
        [Arguments(typeof(int?))] // nullable primitive
        [Arguments(typeof(Enum))] // enum
        [Arguments(typeof(SomeEnum))] // custom enum
        [Arguments(typeof(Guid))] // built-in value type
        [Arguments(typeof(SomeValueType?))] // nullable custom value type
        [Arguments(typeof(string))] // built-in reference type
        [Arguments(typeof(ClassWithNoConverter))] // no converter
        [Arguments(typeof(DerivedClass))] // derived class converter
        [Arguments(typeof(IDerived))] // derived interface
        [Arguments(typeof(ClassIDerived))] // class which implements derived interface 
        public TypeConverter GetConverter(Type typeToConvert) => TypeDescriptor.GetConverter(typeToConvert);
    }
}
