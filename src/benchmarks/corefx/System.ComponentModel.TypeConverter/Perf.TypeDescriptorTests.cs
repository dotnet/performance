// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.ComponentModel.Tests
{
    public class Perf_TypeDescriptorTests
    {
        [Benchmark]
        [Arguments(typeof(int), typeof(Int32Converter))] // primitive
        [Arguments(typeof(int?), typeof(NullableConverter))] // nullable primitive
        [Arguments(typeof(Enum), typeof(EnumConverter))] // enum
        [Arguments(typeof(SomeEnum), typeof(EnumConverter))] // custom enum
        [Arguments(typeof(Guid), typeof(GuidConverter))] // built-in value type
        [Arguments(typeof(SomeValueType?), typeof(NullableConverter))] // nullable custom value type
        [Arguments(typeof(string), typeof(StringConverter))] // built-in reference type
        [Arguments(typeof(ClassWithNoConverter), typeof(TypeConverter))] // no converter
        [Arguments(typeof(DerivedClass), typeof(DerivedClassConverter))] // derived class converter
        [Arguments(typeof(IDerived), typeof(IBaseConverter))] // derived interface
        [Arguments(typeof(ClassIDerived), typeof(IBaseConverter))] // class which implements derived interface 
        public TypeConverter GetConverter(Type typeToConvert, Type expectedConverter) // the expectedConverter argument is not used anymore, but kept to remain BenchView ID, do NOT remove
        {
            TypeConverter converter = default;
            
            for (int i = 0; i < 100; i++)
            {
                converter = TypeDescriptor.GetConverter(typeToConvert);
            }

            return converter;
        }
    }
}
