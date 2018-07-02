// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using BenchmarkDotNet.Attributes;

namespace System.ComponentModel.Tests
{
    public class Perf_TypeDescriptorTests
    {
        [Benchmark]
        [Arguments(typeof(bool), typeof(BooleanConverter))]
        [Arguments(typeof(byte), typeof(ByteConverter))]
        [Arguments(typeof(SByte), typeof(SByteConverter))]
        [Arguments(typeof(char), typeof(CharConverter))]
        [Arguments(typeof(double), typeof(DoubleConverter))]
        [Arguments(typeof(string), typeof(StringConverter))]
        [Arguments(typeof(short), typeof(Int16Converter))]
        [Arguments(typeof(int), typeof(Int32Converter))]
        [Arguments(typeof(long), typeof(Int64Converter))]
        [Arguments(typeof(float), typeof(SingleConverter))]
        [Arguments(typeof(UInt16), typeof(UInt16Converter))]
        [Arguments(typeof(UInt32), typeof(UInt32Converter))]
        [Arguments(typeof(UInt64), typeof(UInt64Converter))]
        [Arguments(typeof(object), typeof(TypeConverter))]
        [Arguments(typeof(void), typeof(TypeConverter))]
        [Arguments(typeof(DateTime), typeof(DateTimeConverter))]
        [Arguments(typeof(DateTimeOffset), typeof(DateTimeOffsetConverter))]
        [Arguments(typeof(Decimal), typeof(DecimalConverter))]
        [Arguments(typeof(TimeSpan), typeof(TimeSpanConverter))]
        [Arguments(typeof(Guid), typeof(GuidConverter))]
        [Arguments(typeof(Array), typeof(ArrayConverter))]
        [Arguments(typeof(ICollection), typeof(CollectionConverter))]
        [Arguments(typeof(Enum), typeof(EnumConverter))]
        [Arguments(typeof(SomeEnum), typeof(EnumConverter))]
        [Arguments(typeof(SomeValueType?), typeof(NullableConverter))]
        [Arguments(typeof(int?), typeof(NullableConverter))]
        [Arguments(typeof(ClassWithNoConverter), typeof(TypeConverter))]
        [Arguments(typeof(BaseClass), typeof(BaseClassConverter))]
        [Arguments(typeof(DerivedClass), typeof(DerivedClassConverter))]
        [Arguments(typeof(IBase), typeof(IBaseConverter))]
        [Arguments(typeof(IDerived), typeof(IBaseConverter))]
        [Arguments(typeof(ClassIBase), typeof(IBaseConverter))]
        [Arguments(typeof(ClassIDerived), typeof(IBaseConverter))]
        [Arguments(typeof(Uri), typeof(UriTypeConverter))]
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
