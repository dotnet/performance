// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Reporting;

public class Test
{
    public IList<string> Categories { get; set; } = [];

    public string Name { get; set; } = "Test";

    public Dictionary<string, string> AdditionalData { get; set; } = [];

    public IList<Counter> Counters { get; set; } = [];

    public void AddCounter(Counter counter)
    {
        counter.Validate();

        if (counter.DefaultCounter && !counter.TopCounter)
        {
            throw new InvalidOperationException($"Default counter '{counter.Name}' must also be a top counter.");
        }

        if (counter.DefaultCounter && Counters.Any(c => c.DefaultCounter))
        {
            throw new Exception($"Duplicate default counter, name: ${counter.Name}");
        }

        if (Counters.Any(c => c.Name.Equals(counter.Name)))
        {
            throw new Exception($"Duplicate counter name, name: ${counter.Name}");
        }

        Counters.Add(counter);
    }

    public void AddCounters(IEnumerable<Counter> counters)
    {
        foreach (var counter in counters)
        {
            AddCounter(counter);
        }
    }

    internal void Validate()
    {
        var defaultCounters = Counters.Where(c => c.DefaultCounter).ToList();
        if (defaultCounters.Count != 1)
        {
            throw new InvalidOperationException($"Test '{Name}' must have exactly one default counter, but found {defaultCounters.Count}.");
        }

        if (!defaultCounters[0].TopCounter)
        {
            throw new InvalidOperationException($"Default counter '{defaultCounters[0].Name}' must also be a top counter.");
        }

        var duplicateCounter = Counters.GroupBy(c => c.Name).FirstOrDefault(group => group.Count() > 1);
        if (duplicateCounter is not null)
        {
            throw new InvalidOperationException($"Duplicate counter name, name: ${duplicateCounter.Key}");
        }

        foreach (var counter in Counters)
        {
            counter.Validate();
        }
    }

    [OnSerializing]
    private void OnSerializing(StreamingContext _) => Validate();

    [OnDeserialized]
    private void OnDeserialized(StreamingContext _) => Validate();
}
