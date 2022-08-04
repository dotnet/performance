// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace BenchmarkDotNet.Extensions
{
    public static class ValuesGenerator
    {
        private const int Seed = 12345; // we always use the same seed to have repeatable results!

        public static T GetNonDefaultValue<T>()
        {
            if (typeof(T) == typeof(byte)) // we can't use ArrayOfUniqueValues for byte
                return Array<T>(byte.MaxValue).First(value => !value.Equals(default));
            else
                return ArrayOfUniqueValues<T>(2).First(value => !value.Equals(default));
        }

        /// <summary>
        /// does not support byte because there are only 256 unique byte values
        /// </summary>
        public static T[] ArrayOfUniqueValues<T>(int count)
        {
            // allocate the array first to try to take advantage of memory randomization
            // as it's usually the first thing called from GlobalSetup method
            // which with MemoryRandomization enabled is the first method called right after allocation
            // of random-sized memory by BDN engine
            T[] result = new T[count];

            var random = new Random(Seed);

            var uniqueValues = new HashSet<T>();

            while (uniqueValues.Count != count)
            {
                T value = GenerateValue<T>(random);

                if (!uniqueValues.Contains(value))
                    uniqueValues.Add(value);
            }

            uniqueValues.CopyTo(result);

            return result;
        }

        public static T[] Array<T>(int count)
        {
            var result = new T[count];

            var random = new Random(Seed);

            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
            {
                random.NextBytes(Unsafe.As<byte[]>(result));
            }
            else
            {
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = GenerateValue<T>(random);
                }
            }

            return result;
        }

        public static readonly byte[] s_encodingMap = {
            65, 66, 67, 68, 69, 70, 71, 72,         //A..H
            73, 74, 75, 76, 77, 78, 79, 80,         //I..P
            81, 82, 83, 84, 85, 86, 87, 88,         //Q..X
            89, 90, 97, 98, 99, 100, 101, 102,      //Y..Z, a..f
            103, 104, 105, 106, 107, 108, 109, 110, //g..n
            111, 112, 113, 114, 115, 116, 117, 118, //o..v
            119, 120, 121, 122, 48, 49, 50, 51,     //w..z, 0..3
            52, 53, 54, 55, 56, 57, 43, 47          //4..9, +, /
        };

        public static byte[] ArrayBase64EncodingBytes(int count)
        {
            var result = new byte[count];

            var random = new Random(Seed);

            for (int i = 0; i < result.Length; i++)
            {
                int index = random.Next(0, s_encodingMap.Length);
                result[i] = s_encodingMap[index];
            }

            return result;
        }

        public static Dictionary<TKey, TValue> Dictionary<TKey, TValue>(int count)
        {
            var dictionary = new Dictionary<TKey, TValue>();

            var random = new Random(Seed);

            while (dictionary.Count != count)
            {
                TKey key = GenerateValue<TKey>(random);

                if (!dictionary.ContainsKey(key))
                    dictionary.Add(key, GenerateValue<TValue>(random));
            }

            return dictionary;
        }

        public static string[] ArrayOfStrings(int count, int minLength, int maxLength)
        {
            var random = new Random(Seed);

            string[] strings = new string[count];
            for (int i = 0; i < strings.Length; i++)
            {
                strings[i] = GenerateRandomString(random, minLength, maxLength);
            }
            return strings;
        }

        private static T GenerateValue<T>(Random random)
        {
            if (typeof(T) == typeof(byte))
                return (T)(object)(byte)random.Next(byte.MinValue, byte.MaxValue);
            if (typeof(T) == typeof(char))
                return (T)(object)(char)random.Next(char.MinValue, char.MaxValue);
            if (typeof(T) == typeof(short))
                return (T)(object)(short)random.Next(short.MaxValue);
            if (typeof(T) == typeof(ushort))
                return (T)(object)(ushort)random.Next(short.MaxValue);
            if (typeof(T) == typeof(int))
                return (T)(object)random.Next();
            if (typeof(T) == typeof(uint))
                return (T)(object)(uint)random.Next();
            if (typeof(T) == typeof(long))
                return (T)(object)(long)random.Next();
            if (typeof(T) == typeof(ulong))
                return (T)(object)(ulong)random.Next();
            if (typeof(T) == typeof(float))
                return (T)(object)(float)random.NextDouble();
            if (typeof(T) == typeof(double))
                return (T)(object)random.NextDouble();
            if (typeof(T) == typeof(bool))
                return (T)(object)(random.NextDouble() > 0.5);
            if (typeof(T) == typeof(decimal))
                return (T)(object)GenerateRandomDecimal(random);
            if (typeof(T) == typeof(string))
                return (T)(object)GenerateRandomString(random, 1, 50);
            if (typeof(T) == typeof(Guid))
                return (T)(object)GenerateRandomGuid(random);

            throw new NotImplementedException($"{typeof(T).Name} is not implemented");
        }

        private static string GenerateRandomString(Random random, int minLength, int maxLength)
        {
            var length = random.Next(minLength, maxLength);

            var builder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                var rangeSelector = random.Next(0, 3);

                if (rangeSelector == 0)
                    builder.Append((char)random.Next('a', 'z'));
                else if (rangeSelector == 1)
                    builder.Append((char)random.Next('A', 'Z'));
                else
                    builder.Append((char)random.Next('0', '9'));
            }

            return builder.ToString();
        }

        private static Guid GenerateRandomGuid(Random random)
        {
            byte[] bytes = new byte[16];
            random.NextBytes(bytes);
            return new Guid(bytes);
        }

        private static decimal GenerateRandomDecimal(Random random)
        {
            byte scale = (byte)random.Next(29);
            bool sign = random.Next(2) == 0;
            return new decimal(random.NextInt32(),
                               random.NextInt32(),
                               random.NextInt32(),
                               sign,
                               scale);
        }

        private static int NextInt32(this Random random)
        {
            int firstBits = random.Next(0, 1 << 4) << 28;
            int lastBits = random.Next(0, 1 << 28);
            return firstBits | lastBits;
        }
    }
}