﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace System.Collections
{
    internal static class Utils
    {
        internal const int DefaultCollectionSize = 512;
        
        internal const int ConcurrencyLevel = 4;

        internal static void FillArrays<T>(ref T[][] arrays, int collectionsCount, T[] source)
        {
            if (arrays == null)
                arrays = Enumerable.Range(0, collectionsCount).Select(_ => new T[source.Length]).ToArray();

            foreach (var array in arrays)
                Array.Copy(sourceArray: source, destinationArray: array, length: source.Length);

            // we dont use Debug.Assert here because this code will be executed mostly in Release
            if (arrays.Any(collection => collection.Length != source.Length) ||
                arrays.Length != collectionsCount)
                throw new InvalidOperationException();
        }

        internal static void ClearAndFillCollections<TCollection, TValues>(ref TCollection[] collections, int collectionsCount, TValues[] keys)
            where TCollection : ICollection<TValues>, new()
        {
            if (collections != null)
                foreach (var collection in collections)
                   collection.Clear();
            FillCollections(ref collections, collectionsCount, keys);
        }

        internal static void FillCollections<TCollection, TValues>(ref TCollection[] collections, int collectionsCount, TValues[] keys)
            where TCollection : ICollection<TValues>, new()
        {
            if (collections == null)
                collections = Enumerable.Range(0, collectionsCount).Select(_ => new TCollection()).ToArray();

            foreach (var collection in collections.Where(collection => collection.Count < keys.Length))
                foreach (var value in keys)
                    collection.Add(value);

            // we dont use Debug.Assert here because this code will be executed mostly in Release
            if (collections.Any(collection => collection.Count != keys.Length) ||
                collections.Length != collectionsCount)
                throw new InvalidOperationException();
        }

        internal static void FillDictionaries<TCollection, TValues>(ref TCollection[] collections, int collectionsCount, TValues[] keys)
            where TCollection : IDictionary<TValues, TValues>, new()
        {
            if (collections == null)
                collections = Enumerable.Range(0, collectionsCount).Select(_ => new TCollection()).ToArray();

            foreach (var collection in collections.Where(collection => collection.Count < keys.Length))
            foreach (var key in keys)
                collection.Add(key, key);

            // we dont use Debug.Assert here because this code will be executed mostly in Release
            if (collections.Any(collection => collection.Count != keys.Length) ||
                collections.Length != collectionsCount)
                throw new InvalidOperationException();
        }

        internal static void FillProducerConsumerCollection<TCollection, TValues>(ref TCollection[] collections, int collectionsCount, TValues[] keys)
            where TCollection : IProducerConsumerCollection<TValues>, new()
        {
            if (collections == null)
                collections = Enumerable.Range(0, collectionsCount).Select(_ => new TCollection()).ToArray();

            foreach (var collection in collections.Where(collection => collection.Count < keys.Length))
            foreach (var value in keys)
                collection.TryAdd(value);

            // we dont use Debug.Assert here because this code will be executed mostly in Release
            if (collections.Any(collection => collection.Count != keys.Length) ||
                collections.Length != collectionsCount)
                throw new InvalidOperationException();
        }

        internal static void FillStacks<T>(ref Stack<T>[] stacks, int collectionsCount, T[] keys) // Stack<T> does not implement any interface that exposes .Push method
        {
            if (stacks == null)
                stacks = Enumerable.Range(0, collectionsCount).Select(_ => new Stack<T>()).ToArray();

            foreach (var stack in stacks.Where(stack => stack.Count < keys.Length))
            foreach (var value in keys)
                stack.Push(value);

            // we dont use Debug.Assert here because this code will be executed mostly in Release
            if (stacks.Any(collection => collection.Count != keys.Length) ||
                stacks.Length != collectionsCount)
                throw new InvalidOperationException();
        }

        internal static void FillQueues<T>(ref Queue<T>[] queues, int collectionsCount, T[] keys) // Queue<T> does not implement any interface that exposes .Enqueue method
        {
            if (queues == null)
                queues = Enumerable.Range(0, collectionsCount).Select(_ => new Queue<T>()).ToArray();

            foreach (var queue in queues.Where(queue => queue.Count < keys.Length))
            foreach (var value in keys)
                queue.Enqueue(value);

            // we dont use Debug.Assert here because this code will be executed mostly in Release
            if (queues.Any(collection => collection.Count != keys.Length) ||
                queues.Length != collectionsCount)
                throw new InvalidOperationException();
        }
    }
}
