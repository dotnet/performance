// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace BenchmarkDotNet.Extensions
{
    public static class ValuesGenerator
    {
        private const int Seed = 12345; // we always use the same seed to have repeatable results!

        /// <summary>
        /// Returns a T value that is NOT the default(T) (typically zero) value.
        /// For bool, there's only one choice (true/1).
        /// For byte/sbyte, will never return byte.MaxValue (256) so there are a maximum of 254 values.
        /// </summary>
        public static T GetNonDefaultValue<T>()
        {
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte)) // we can't use ArrayOfUniqueValues for byte/sbyte (but they have the same range)
                return Array<T>(byte.MaxValue).First(value => !value.Equals(default(T)));
            else
                return ArrayOfUniqueValues<T>(2).First(value => !value.Equals(default(T)));
        }

        /// <summary>
        /// Returns an array of the requested size where each entry is a distinct value. 
        /// For byte and sbyte support a maximum count of 255 because there are only 256
        /// unique byte values. For bool, there are only 2 values so throw for more requested
        /// </summary>
        public static T[] ArrayOfUniqueValues<T>(int count)
        {
             if (count > 2 && typeof(T) == typeof(bool))
                throw new ArgumentOutOfRangeException("count", "Cannot exceed 2 for bool values");
             if (count > 255 && (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte)))
                throw new ArgumentOutOfRangeException("count", "Cannot exceed 255 for byte or sbyte values");

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

        /// <summary>
        /// Returns an array of the requested size where each entry is a random value and values could repeat 
        /// For byte and sbyte values are built from Random.NextBytes, for other types GenerateValue is used
        /// to generate a random value in the appropriate range
        /// </summary>
        public static T[] Array<T>(int count, int? seed = null)
        {
            var result = new T[count];

            var random = new Random(seed ?? Seed);

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

        public static T Value<T>(int? seed = null)
        {
            var random = new Random(seed ?? Seed);
            return GenerateValue<T>(random);
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

        /// <summary>
        /// Returns an array of bytes of the requested size where each entry is a random value 
        /// from the legal subset of ASCII characters used in Base-64 encoded values
        /// </summary>
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

        /// <summary>
        /// Returns a Dictionary of the requested size where each entry is a has a distinct key value and
        /// the stored values are randomly generated. 
        /// GenerateValue is used to generate a random value in the appropriate range for both the key and value
        /// </summary>
        public static Dictionary<TKey, TValue> Dictionary<TKey, TValue>(int count)
        {
            if (count > 2 && typeof(TKey) == typeof(bool))
                throw new ArgumentOutOfRangeException("count", "Cannot exceed 2 for Dictionary<bool, TValue>");
            if (count > 255 && (typeof(TKey) == typeof(byte) || typeof(TKey) == typeof(sbyte)))
                throw new ArgumentOutOfRangeException("count", "Cannot exceed 255 for Dictionary<byte, TValue> or Dictionary<sbyte, TValue>");

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

        /// <summary>
        /// Returns an array of the requested size where each entry is a random string value.
        /// The values are random length strings within the range requested and are composed
        /// a random assortment of the letters a..z, A..Z, and digits 0..9
        /// </summary>
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

        public static string[] ArrayOfUniqueStrings(int count, int minLength, int maxLength)
        {
            string[] strings = new string[count];

            HashSet<string> unique = new ();
            Random random = new (Seed);
            
            while (unique.Count != count)
            {
                unique.Add(GenerateRandomString(random, minLength, maxLength));
            }
            unique.CopyTo(strings);
            return strings;
        }

        /// <summary>
        /// Returns a random of the type requested
        /// For strings, it will be a random assortment of the letters 'a'..'z', 'A'..'Z', or '0'..'9' that is 1 to 50 characters long
        /// </summary>
        private static T GenerateValue<T>(Random random)
        {
            // Note: some of these types (especially the unsigned and values larger than int) are not giving the full range of values.
            // WE CANNOT change that now because existing performance tests are based on the values returned previously.
            if (typeof(T) == typeof(byte))
                return (T)(object)(byte)random.Next(byte.MinValue, byte.MaxValue);      // note: will never return 256 as Random.Next max value is exclusive
            if (typeof(T) == typeof(sbyte))
                return (T)(object)(sbyte)random.Next(sbyte.MinValue, sbyte.MaxValue);   // note: will never return 128 as Random.Next max value is exclusive
            if (typeof(T) == typeof(char))
                return (T)(object)(char)random.Next(char.MinValue, char.MaxValue);      // note: will never return `\uffff` as Random.Next max value is exclusive
            if (typeof(T) == typeof(short))
                return (T)(object)(short)random.Next(short.MaxValue);   // note: will never return short.MaxValue as Random.Next max value is exclusive
            if (typeof(T) == typeof(ushort))
                return (T)(object)(ushort)random.Next(short.MaxValue);  // note: will never return short.MaxValue (right in the middle of the domain)
            if (typeof(T) == typeof(int))
                return (T)(object)random.Next();         // note: cannot call NextInt32 here, because that would change what we've returned in the past
            if (typeof(T) == typeof(uint))
                return (T)(object)(uint)random.Next();   // note: cannot call NextInt32 here, because that would change what we've returned in the past
            if (typeof(T) == typeof(long))
                return (T)(object)(long)random.Next();   // note: will never return a value at or above int.MaxValue
            if (typeof(T) == typeof(ulong))
                return (T)(object)(ulong)random.Next();  // note: will never return a value at or above int.MaxValue because Random.Next never returns negatives
            if (typeof(T) == typeof(float))
                return (T)(object)(float)random.NextDouble();
            if (typeof(T) == typeof(double))
                return (T)(object)random.NextDouble();
            if (typeof(T) == typeof(bool))
                return (T)(object)(random.NextDouble() > 0.5);
            if (typeof(T) == typeof(decimal))
                return (T)(object)GenerateRandomDecimal(random);
            if (typeof(T) == typeof(string))
                return (T)(object)GenerateRandomString(random, 1, 50);  // note: all strings have only the characters 'a'..'z', 'A'..'Z', or '0'..'9'
            if (typeof(T) == typeof(Guid))
                return (T)(object)GenerateRandomGuid(random);   // note: may return malformed Guids (not logically valid per RFC 4122 formatting)

            if (typeof(T).GetConstructor(new[] { typeof(int) }) is ConstructorInfo ctor)
                return (T)ctor.Invoke(new[] { (object)random.Next() });

            throw new NotImplementedException($"{typeof(T).Name} is not implemented");
        }
     
        /// <summary>
        /// Returns a random length strings within the range requested and are composed
        /// a random assortment of the letters a..z, A..Z, and digits 0..9
        /// </summary>
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

        /// <summary>
        /// Returns a randomly generated Guid.
        /// Note: this is not guaranteed to be a reasonably formatted RFC 4122 Guid, NOR is this
        /// necessarily a "Version 4 Random" guid. 
        /// </summary>
        private static Guid GenerateRandomGuid(Random random)
        {
            byte[] bytes = new byte[16];
            random.NextBytes(bytes);
            return new Guid(bytes);
        }

        /// <summary>
        /// Returns a randomly generated decimal value. 
        /// Note: returns a value across the entire valid decimal value-space
        /// </summary>
        private static decimal GenerateRandomDecimal(Random random)
        {
            // Decimal values have a sign, a scale, and 96 bits of significance (lo/mid/high)
            // generate those parts randomly and assemble a valid decimal
            byte scale = (byte)random.Next(29);
            bool sign = random.Next(2) == 0;
            return new decimal(random.NextInt32(),
                               random.NextInt32(),
                               random.NextInt32(),
                               sign,
                               scale);
        }
       
        /// <summary>
        /// Returns a randomly generated int value from the entire range of legal 32-bit integers
        /// Note: using Random.Next will never return negative values so we can't use that where
        /// we want the complete bit-space.
        /// WARNING: Do not use this method in the GenerateValue for the various int-like types
        /// as that would change the range and order of values returned and THAT would change the
        /// performance benchmarks
        /// </summary>
        private static int NextInt32(this Random random)
        {
            int firstBits = random.Next(0, 1 << 4) << 28;
            int lastBits = random.Next(0, 1 << 28);
            return firstBits | lastBits;
        }
    }
}