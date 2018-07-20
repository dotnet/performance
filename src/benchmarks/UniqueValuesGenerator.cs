using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Benchmarks
{
    public static class UniqueValuesGenerator
    {
        private const int Seed = 12345; // we always use the same seed to have repeatable results!

        public static T[] GenerateArray<T>(int count)
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

        public static Dictionary<TKey, TValue> GenerateDictionary<TKey, TValue>(int count)
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
            if (typeof(T) == typeof(int))
                return (T)(object)random.Next();
            if (typeof(T) == typeof(double))
                return (T)(object)random.NextDouble();
            if (typeof(T) == typeof(string))
                return (T) (object) GenerateRandomString(random, 1, 50);
            
            throw new NotImplementedException($"{typeof(T).Name} is not implemented");
        }

        private static string GenerateRandomString(Random random, int minLength, int maxLength)
        {
            var length = random.Next(minLength, maxLength);

            var builder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                builder.Append((char) random.Next(char.MinValue, char.MaxValue));

            return builder.ToString();
        }
    }
}