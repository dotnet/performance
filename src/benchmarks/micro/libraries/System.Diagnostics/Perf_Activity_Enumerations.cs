// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;

namespace System.Diagnostics
{
    [MemoryDiagnoser]
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Activity_Enumerations
    {
        private static readonly ActivitySource s_ActivitySource;
        private static readonly ActivityListener s_ActivityListener;
        private static readonly Activity s_ActivitySmall;
        private static readonly Activity s_ActivityLarge;
        private static readonly ActivityLink s_ActivityLinkSmall;
        private static readonly ActivityLink s_ActivityLinkLarge;

        static Perf_Activity_Enumerations()
        {
            s_ActivitySource = new ActivitySource("TestActivitySource");

            s_ActivityListener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "TestActivitySource",

                Sample = (ref ActivityCreationOptions<ActivityContext> o) => ActivitySamplingResult.AllDataAndRecorded
            };

            Dictionary<string, object> LargeTagSet = new Dictionary<string, object>();
            for (int i = 0; i < 1024; i++)
            {
                if (i % 2 == 0)
                    LargeTagSet.Add($"Key{i}", i);
                else
                    LargeTagSet.Add($"Key{i}", i.ToString());
            }

            ActivitySource.AddActivityListener(s_ActivityListener);

            s_ActivitySmall = s_ActivitySource.StartActivity(
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

            s_ActivitySmall.AddEvent(new ActivityEvent("TestEvent1"));
            s_ActivitySmall.AddEvent(new ActivityEvent("TestEvent2"));
            s_ActivitySmall.AddEvent(new ActivityEvent("TestEvent3"));
            s_ActivitySmall.AddEvent(new ActivityEvent("TestEvent4"));

            s_ActivitySmall.Stop();

            ActivityLink[] LargeLinkSet = new ActivityLink[1024];
            for (int i = 0; i < 1024; i++)
            {
                LargeLinkSet[i] = new ActivityLink(default);
            }

            s_ActivityLarge = s_ActivitySource.StartActivity(
                "TestActivity",
                ActivityKind.Internal,
                parentContext: default,
                tags: LargeTagSet,
                links: LargeLinkSet);

            for (int i = 0; i < 1024; i++)
            {
                s_ActivityLarge.AddEvent(new ActivityEvent($"TestEvent{i}"));
            }

            s_ActivityLarge.Stop();

            s_ActivityLinkSmall = new ActivityLink(
                default,
                new ActivityTagsCollection(
                    new Dictionary<string, object>
                    {
                        ["tag1"] = "string1",
                        ["tag2"] = 1,
                        ["tag3"] = "string2",
                        ["tag4"] = false,
                    }));

            s_ActivityLinkLarge = new ActivityLink(
                default,
                new ActivityTagsCollection(LargeTagSet));
        }

        [Benchmark]
        public void EnumerateActivityTagsSmall()
        {
            foreach (var _ in s_ActivitySmall.Tags)
            {
            }
        }

        [Benchmark]
        public void EnumerateActivityTagsLarge()
        {
            foreach (var _ in s_ActivityLarge.Tags)
            {
            }
        }

        [Benchmark]
        public void EnumerateActivityTagObjectsSmall()
        {
            foreach (var _ in s_ActivitySmall.TagObjects)
            {
            }
        }

        [Benchmark]
        public void EnumerateActivityTagObjectsLarge()
        {
            foreach (var _ in s_ActivityLarge.TagObjects)
            {
            }
        }

        [Benchmark]
        public void EnumerateActivityLinksSmall()
        {
            foreach (var _ in s_ActivitySmall.Links)
            {
            }
        }

        [Benchmark]
        public void EnumerateActivityLinksLarge()
        {
            foreach (var _ in s_ActivityLarge.Links)
            {
            }
        }

        [Benchmark]
        public void EnumerateActivityEventsSmall()
        {
            foreach (var _ in s_ActivitySmall.Events)
            {
            }
        }

        [Benchmark]
        public void EnumerateActivityEventsLarge()
        {
            foreach (var _ in s_ActivityLarge.Events)
            {
            }
        }

        [Benchmark]
        public void EnumerateActivityLinkTagsSmall()
        {
            foreach (var _ in s_ActivityLinkSmall.Tags)
            {
            }
        }

        [Benchmark]
        public void EnumerateActivityLinkTagsLarge()
        {
            foreach (var _ in s_ActivityLinkLarge.Tags)
            {
            }
        }
    }
}
