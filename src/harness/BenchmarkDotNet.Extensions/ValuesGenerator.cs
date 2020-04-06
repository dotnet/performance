﻿// Licensed to the .NET Foundation under one or more agreements.
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
            var random = new Random(Seed); 

            var uniqueValues = new HashSet<T>();

            while (uniqueValues.Count != count)
            {
                T value = GenerateValue<T>(random);

                if (!uniqueValues.Contains(value))
                    uniqueValues.Add(value);
            }

            return uniqueValues.ToArray();
        }
        
        public static T[] Array<T>(int count)
        {
            var random = new Random(Seed); 

            var result = new T[count];

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

        public static Dictionary<TKey, TValue> Dictionary<TKey, TValue>(int count)
        {
            var random = new Random(Seed);

            var dictionary = new Dictionary<TKey, TValue>();

            while (dictionary.Count != count)
            {
                TKey key = GenerateValue<TKey>(random);

                if (!dictionary.ContainsKey(key))
                    dictionary.Add(key, GenerateValue<TValue>(random));
            }

            return dictionary;
        }

        private static T GenerateValue<T>(Random random)
        {
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
            if (typeof(T) == typeof(string))
                return (T)(object)GenerateRandomString(random, 1, 50);

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
                    builder.Append((char) random.Next('a', 'z'));
                else if (rangeSelector == 1)
                    builder.Append((char) random.Next('A', 'Z'));
                else
                    builder.Append((char) random.Next('0', '9'));
            }

            return builder.ToString();
        }
    }
}