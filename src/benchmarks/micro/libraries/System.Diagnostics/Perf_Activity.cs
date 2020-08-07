// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Diagnostics
{
    [MemoryDiagnoser]
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Activity : IDisposable
    {
        private readonly ActivitySource _ActivitySource;
        private readonly ActivityListener _ActivityListener;
        private readonly Activity _Activity;
        private readonly ActivityLink _ActivityLink;

        public Perf_Activity()
        {
            _ActivitySource = new ActivitySource("TestActivitySource");

            _ActivityListener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "TestActivitySource",

                GetRequestedDataUsingContext = (ref ActivityCreationOptions<ActivityContext> o) => ActivityDataRequest.AllDataAndRecorded
            };

            ActivitySource.AddActivityListener(_ActivityListener);

            _Activity = _ActivitySource.StartActivity(
                "TestActivity",
                ActivityKind.Internal,
                parentContext: default,
                tags: new Dictionary<string, object>
                {
                    ["tag1"] = "string1",
                    ["tag2"] = 1,
                    ["tag3"] = "string2",
                    ["tag4"] = false,
                },
                links: new ActivityLink[]
                {
                    new ActivityLink(default),
                    new ActivityLink(default),
                    new ActivityLink(default),
                    new ActivityLink(default),
                });

            _Activity.AddEvent(new ActivityEvent("TestEvent1"));
            _Activity.AddEvent(new ActivityEvent("TestEvent2"));
            _Activity.AddEvent(new ActivityEvent("TestEvent3"));
            _Activity.AddEvent(new ActivityEvent("TestEvent4"));

            _Activity.Stop();

            _ActivityLink = new ActivityLink(
                default,
                new ActivityTagsCollection(
                    new Dictionary<string, object>
                    {
                        ["tag1"] = "string1",
                        ["tag2"] = 1,
                        ["tag3"] = "string2",
                        ["tag4"] = false,
                    }));
        }

        public void Dispose()
        {
            _ActivityListener.Dispose();
            _ActivitySource.Dispose();
        }

        [Params(5000)]
        public int NumberOfActivities { get; set; }

        [Benchmark]
        public void EnumerateActivityTags()
        {
            int total = 0;

            for (int i = 0; i < NumberOfActivities; i++)
            {
                foreach (var tagString in _Activity.Tags)
                {
                    total++;
                }
            }
        }

        [Benchmark]
        public void EnumerateActivityTagObjects()
        {
            int total = 0;

            for (int i = 0; i < NumberOfActivities; i++)
            {
                foreach (var tagObject in _Activity.TagObjects)
                {
                    total++;
                }
            }
        }

        [Benchmark]
        public void OTelHelperActivityTagObjects()
        {
            int total = 0;

            Enumerator<IEnumerable<KeyValuePair<string, object>>, KeyValuePair<string, object>, int>.AllocationFreeForEachDelegate foreachDelegate
                = Enumerator<IEnumerable<KeyValuePair<string, object>>, KeyValuePair<string, object>, int>.FindAllocationFreeForEach(_Activity.TagObjects);

            for (int i = 0; i < NumberOfActivities; i++)
            {
                foreachDelegate(_Activity.TagObjects, ref total, ForeachTagObjectRef);
            }
        }

        private static readonly Enumerator<IEnumerable<KeyValuePair<string, object>>, KeyValuePair<string, object>, int>.ForEachDelegate ForeachTagObjectRef = ForeachTagObject;

        private static bool ForeachTagObject(ref int total, KeyValuePair<string, object> tag)
        {
            total++;
            return true;
        }

        [Benchmark]
        public void EnumerateActivityLinks()
        {
            int total = 0;

            for (int i = 0; i < NumberOfActivities; i++)
            {
                foreach (var link in _Activity.Links)
                {
                    total++;
                }
            }
        }

        [Benchmark]
        public void OTelHelperActivityLinks()
        {
            int total = 0;

            Enumerator<IEnumerable<ActivityLink>, ActivityLink, int>.AllocationFreeForEachDelegate foreachDelegate
                = Enumerator<IEnumerable<ActivityLink>, ActivityLink, int>.FindAllocationFreeForEach(_Activity.Links);

            for (int i = 0; i < NumberOfActivities; i++)
            {
                foreachDelegate(_Activity.Links, ref total, ForeachLinkRef);
            }
        }

        private static readonly Enumerator<IEnumerable<ActivityLink>, ActivityLink, int>.ForEachDelegate ForeachLinkRef = ForeachLink;

        private static bool ForeachLink(ref int total, ActivityLink link)
        {
            total++;
            return true;
        }

        [Benchmark]
        public void EnumerateActivityEvents()
        {
            int total = 0;

            for (int i = 0; i < NumberOfActivities; i++)
            {
                foreach (var @event in _Activity.Events)
                {
                    total++;
                }
            }
        }

        [Benchmark]
        public void OTelHelperActivityEvents()
        {
            int total = 0;

            Enumerator<IEnumerable<ActivityEvent>, ActivityEvent, int>.AllocationFreeForEachDelegate foreachDelegate
                = Enumerator<IEnumerable<ActivityEvent>, ActivityEvent, int>.FindAllocationFreeForEach(_Activity.Events);

            for (int i = 0; i < NumberOfActivities; i++)
            {
                foreachDelegate(_Activity.Events, ref total, ForeachEventRef);
            }
        }

        private static readonly Enumerator<IEnumerable<ActivityEvent>, ActivityEvent, int>.ForEachDelegate ForeachEventRef = ForeachEvent;

        private static bool ForeachEvent(ref int total, ActivityEvent @event)
        {
            total++;
            return true;
        }

        [Benchmark]
        public void EnumerateActivityLinkTags()
        {
            int total = 0;

            for (int i = 0; i < NumberOfActivities; i++)
            {
                foreach (var tag in _ActivityLink.Tags)
                {
                    total++;
                }
            }
        }

        [Benchmark]
        public void OTelHelperActivityLinkTags()
        {
            int total = 0;

            Enumerator<IEnumerable<KeyValuePair<string, object>>, KeyValuePair<string, object>, int>.AllocationFreeForEachDelegate foreachDelegate
                = Enumerator<IEnumerable<KeyValuePair<string, object>>, KeyValuePair<string, object>, int>.FindAllocationFreeForEach(_ActivityLink.Tags);

            for (int i = 0; i < NumberOfActivities; i++)
            {
                foreachDelegate(_ActivityLink.Tags, ref total, ForeachTagObjectRef);
            }
        }

        // A helper class for enumerating over IEnumerable<TItem> without allocation if a struct enumerator is available.
        private class Enumerator<TEnumerable, TItem, TState>
            where TEnumerable : IEnumerable<TItem>
            where TState : struct
        {
            private static readonly MethodInfo GenericGetEnumeratorMethod = typeof(IEnumerable<TItem>).GetMethod("GetEnumerator");
            private static readonly MethodInfo GeneircCurrentGetMethod = typeof(IEnumerator<TItem>).GetProperty("Current").GetMethod;
            private static readonly MethodInfo MoveNextMethod = typeof(IEnumerator).GetMethod("MoveNext");
            private static readonly MethodInfo DisposeMethod = typeof(IDisposable).GetMethod("Dispose");
            private static readonly ConcurrentDictionary<Type, AllocationFreeForEachDelegate> AllocationFreeForEachDelegates = new ConcurrentDictionary<Type, AllocationFreeForEachDelegate>();
            private static readonly Func<Type, AllocationFreeForEachDelegate> BuildAllocationFreeForEachDelegateRef = BuildAllocationFreeForEachDelegate;

            public delegate void AllocationFreeForEachDelegate(TEnumerable instance, ref TState state, ForEachDelegate itemCallback);

            public delegate bool ForEachDelegate(ref TState state, TItem item);

            protected Enumerator()
            {
            }

            public static AllocationFreeForEachDelegate FindAllocationFreeForEach(TEnumerable instance)
            {
                var type = instance.GetType();

                return AllocationFreeForEachDelegates.GetOrAdd(
                    type,
                    BuildAllocationFreeForEachDelegateRef);
            }

            /* We want to do this type of logic...
                public static void AllocationFreeForEach(Dictionary<string, int> dictionary, ref TState state, ForEachDelegate itemCallback)
                {
                    using (Dictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            if (!itemCallback(ref state, enumerator.Current))
                                break;
                        }
                    }
                }
                ...because it takes advantage of the struct Enumerator on the built-in types which give an allocation-free way to enumerate.
            */
            private static AllocationFreeForEachDelegate BuildAllocationFreeForEachDelegate(Type enumerableType)
            {
                var itemCallbackType = typeof(ForEachDelegate);

                var getEnumeratorMethod = ResolveGetEnumeratorMethodForType(enumerableType);
                if (getEnumeratorMethod == null)
                {
                    // Fallback to allocation mode and use IEnumerable<TItem>.GetEnumerator.
                    // Primarily for Array.Empty and Enumerable.Empty case, but also for user types.
                    getEnumeratorMethod = GenericGetEnumeratorMethod;
                }

                var enumeratorType = getEnumeratorMethod.ReturnType;

                var dynamicMethod = new DynamicMethod(
                    nameof(FindAllocationFreeForEach),
                    null,
                    new[] { typeof(TEnumerable), typeof(TState).MakeByRefType(), itemCallbackType },
                    typeof(AllocationFreeForEachDelegate).Module,
                    skipVisibility: true);

                var generator = dynamicMethod.GetILGenerator();

                generator.DeclareLocal(enumeratorType);

                var beginLoopLabel = generator.DefineLabel();
                var processCurrentLabel = generator.DefineLabel();
                var returnLabel = generator.DefineLabel();
                var breakLoopLabel = generator.DefineLabel();

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, getEnumeratorMethod);
                generator.Emit(OpCodes.Stloc_0);

                // try
                generator.BeginExceptionBlock();
                {
                    generator.Emit(OpCodes.Br_S, beginLoopLabel);

                    generator.MarkLabel(processCurrentLabel);

                    generator.Emit(OpCodes.Ldarg_2);
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldloca_S, 0);
                    generator.Emit(OpCodes.Constrained, enumeratorType);
                    generator.Emit(OpCodes.Callvirt, GeneircCurrentGetMethod);

                    generator.Emit(OpCodes.Callvirt, itemCallbackType.GetMethod("Invoke"));

                    generator.Emit(OpCodes.Brtrue_S, beginLoopLabel);

                    generator.Emit(OpCodes.Leave_S, returnLabel);

                    generator.MarkLabel(beginLoopLabel);

                    generator.Emit(OpCodes.Ldloca_S, 0);
                    generator.Emit(OpCodes.Constrained, enumeratorType);
                    generator.Emit(OpCodes.Callvirt, MoveNextMethod);

                    generator.Emit(OpCodes.Brtrue_S, processCurrentLabel);

                    generator.MarkLabel(breakLoopLabel);

                    generator.Emit(OpCodes.Leave_S, returnLabel);
                }

                // finally
                generator.BeginFinallyBlock();
                {
                    if (typeof(IDisposable).IsAssignableFrom(enumeratorType))
                    {
                        generator.Emit(OpCodes.Ldloca_S, 0);
                        generator.Emit(OpCodes.Constrained, enumeratorType);
                        generator.Emit(OpCodes.Callvirt, DisposeMethod);
                    }
                }

                generator.EndExceptionBlock();

                generator.MarkLabel(returnLabel);

                generator.Emit(OpCodes.Ret);

                return (AllocationFreeForEachDelegate)dynamicMethod.CreateDelegate(typeof(AllocationFreeForEachDelegate));
            }

            private static MethodInfo ResolveGetEnumeratorMethodForType(Type type)
            {
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var method in methods)
                {
                    if (method.Name == "GetEnumerator" && !method.ReturnType.IsInterface)
                    {
                        return method;
                    }
                }

                return null;
            }
        }
    }
}
