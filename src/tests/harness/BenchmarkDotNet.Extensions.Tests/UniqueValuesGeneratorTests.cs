// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using BenchmarkDotNet.Extensions;
using Xunit;

namespace Tests
{
    public class UniqueValuesGeneratorTests
    {
        [Fact]
        public void UnsupportedTypesThrow()
            => Assert.Throws<NotImplementedException>(() => ValuesGenerator.ArrayOfUniqueValues<UniqueValuesGeneratorTests>(1));

        [Fact]
        public void GeneratedArraysContainOnlyUniqueValues()
        {
            AssertGeneratedArraysContainOnlyUniqueValues<bool>(2);
            AssertGeneratedArraysContainOnlyUniqueValues<byte>(255);
            AssertGeneratedArraysContainOnlyUniqueValues<int>(1024);
            AssertGeneratedArraysContainOnlyUniqueValues<string>(1024);
        }

        private void AssertGeneratedArraysContainOnlyUniqueValues<T>(int size)
        {
            var generatedArray = ValuesGenerator.ArrayOfUniqueValues<T>(size);

            var distinct = generatedArray.Distinct().ToArray();

            Assert.Equal(distinct, generatedArray);
        }

        [Fact]
        public void GeneratedArraysContainAlwaysSameValues()
        {
            AssertGeneratedArraysContainAlwaysSameValues<byte>(255);
            AssertGeneratedArraysContainAlwaysSameValues<int>(1024);
            AssertGeneratedArraysContainAlwaysSameValues<string>(1024);
        }

        private void AssertGeneratedArraysContainAlwaysSameValues<T>(int size)
        {
            var generatedArray = ValuesGenerator.ArrayOfUniqueValues<T>(size);

            for (int i = 0; i < 10; i++)
            {
                var anotherGeneratedArray = ValuesGenerator.ArrayOfUniqueValues<T>(size);

                Assert.Equal(generatedArray, anotherGeneratedArray);
            }
        }

        [Fact]
        public void GeneratedStringsContainOnlySimpleLettersAndDigits()
        {
            var strings = ValuesGenerator.ArrayOfUniqueValues<string>(1024);

            foreach (var text in strings)
            {
                Assert.All(text, character =>
                    Assert.True(
                        char.IsDigit(character)
                        || (character >= 'a' && character <= 'z')
                        || (character >= 'A' && character <= 'Z')));
            }
        }

        [Fact]
        public void GetNonDefaultValueReturnsNonDefaultValue()
        {
            Assert.True(ValuesGenerator.GetNonDefaultValue<bool>());
            Assert.NotEqual(default(byte), ValuesGenerator.GetNonDefaultValue<byte>());
            Assert.NotEqual(default(int), ValuesGenerator.GetNonDefaultValue<int>());
            Assert.NotEqual(default(string), ValuesGenerator.GetNonDefaultValue<string>());
        }

        [Fact]
        public void SupportsByte() => Supports<byte>(255);  // note: testing to a maximum of 255 because byte.MaxValue is never generated

        [Fact]
        public void ThrowsOnTooManyBytes() => Assert.Throws<ArgumentOutOfRangeException>(() => Supports<byte>(256));

        [Fact]
        public void SupportsSignedByte() => Supports<sbyte>(127);  // note: testing to a maximum of 127 because sbyte.MaxValue is never generated

        [Fact]
        public void ThrowsOnTooManySignedBytes() => Assert.Throws<ArgumentOutOfRangeException>(() => Supports<sbyte>(256));

        [Fact]
        public void SupportsChar() => Supports<char>();

        [Fact]
        public void SupportsShort() => Supports<short>();

        [Fact]
        public void SupportsUnsignedShort() => Supports<ushort>();

        [Fact]
        public void SupportsInteger() => Supports<int>();

        [Fact]
        public void SupportsUnsignedInteger() => Supports<uint>();

        [Fact]
        public void SupportsLong() => Supports<long>();

        [Fact]
        public void SupportsUnsignedLong() => Supports<ulong>();

        [Fact]
        public void SupportsFloat() => Supports<float>();

        [Fact]
        public void SupportsDouble() => Supports<double>();

        [Fact]
        public void SupportsBool() => Supports<bool>(2);

        [Fact]
        public void ThrowsOnTooManyBools() => Assert.Throws<ArgumentOutOfRangeException>(() => Supports<bool>(3));

        [Fact]
        public void SupportsDecimal() => Supports<decimal>();

        [Fact]
        public void SupportsString() => Supports<string>();

        [Fact]
        public void SupportsGuid() => Supports<Guid>();

        private static void SupportsArray<T>(int count)
        {
            var array = ValuesGenerator.Array<T>(count);
            Assert.NotNull(array);
            Assert.Equal(count, array.Length);
        }

        private static void SupportsArrayOfUniques<T>(int count)
        {
            var array = ValuesGenerator.ArrayOfUniqueValues<T>(count);
            Assert.NotNull(array);
            Assert.Equal(count, array.Length);
        }

        private static void SupportsNonDefaultValue<T>()
        {
            var value = ValuesGenerator.GetNonDefaultValue<T>();
            Assert.NotEqual(default, value);
        }

        private static void SupportsDictionary<TKey, TValue>(int count)
        {
            var dictionary = ValuesGenerator.Dictionary<TKey, TValue>(count);
            Assert.NotNull(dictionary);
            Assert.Equal(count, dictionary.Count);
        }

        private static void Supports<T>(int count = 10)
        {
            SupportsArray<T>(count);
            SupportsNonDefaultValue<T>();
            SupportsArrayOfUniques<T>(count);
            SupportsDictionary<T, T>(count);
            SupportsDictionary<T, string>(count);
        }
    }
}
