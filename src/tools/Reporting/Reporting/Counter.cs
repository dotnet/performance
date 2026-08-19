// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Reporting;

public enum CounterDirection
{
    Unknown,
    LowerIsBetter,
    HigherIsBetter
}

public class Counter
{
    private const double MaximumRegressionThreshold = 1.0;
    private CounterDirection _direction = CounterDirection.LowerIsBetter;
    private double? _regressionThreshold;

    public string Name { get; set; } = "Counter";

    public bool TopCounter { get; set; }

    public bool DefaultCounter { get; set; }

    [JsonIgnore]
    public bool HigherIsBetter
    {
        get => _direction switch
        {
            CounterDirection.HigherIsBetter => true,
            CounterDirection.LowerIsBetter => false,
            _ => throw new InvalidOperationException($"Counter '{Name}' has an unknown direction.")
        };
        set => _direction = value ? CounterDirection.HigherIsBetter : CounterDirection.LowerIsBetter;
    }

    [JsonIgnore]
    public CounterDirection Direction
    {
        get => _direction;
        set
        {
            if (value is not CounterDirection.Unknown and not CounterDirection.LowerIsBetter and not CounterDirection.HigherIsBetter)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown counter direction.");
            }

            _direction = value;
        }
    }

    [JsonProperty("higherIsBetter", NullValueHandling = NullValueHandling.Include)]
    private bool? SerializedHigherIsBetter
    {
        get => _direction == CounterDirection.Unknown ? null : _direction == CounterDirection.HigherIsBetter;
        set => _direction = value switch
        {
            true => CounterDirection.HigherIsBetter,
            false => CounterDirection.LowerIsBetter,
            null => CounterDirection.Unknown
        };
    }

    public string MetricName { get; set; } = "Count";

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double? RegressionThreshold
    {
        get => _regressionThreshold;
        set
        {
            if (value is double threshold &&
                (double.IsNaN(threshold) || double.IsInfinity(threshold) || threshold <= 0 || threshold > MaximumRegressionThreshold))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Regression threshold must be finite and in the range (0, {MaximumRegressionThreshold}].");
            }

            _regressionThreshold = value;
        }
    }

    public IList<double>? Results { get; set; }

    internal void Validate()
    {
        if (_direction == CounterDirection.Unknown && (DefaultCounter || TopCounter))
        {
            throw new InvalidOperationException($"Counter '{Name}' must have a known direction when it is a default or top counter.");
        }
    }

    public override string ToString() => $"{nameof(Name)}: {Name}, {nameof(TopCounter)}: {TopCounter}, {nameof(DefaultCounter)}: {DefaultCounter}, {nameof(MetricName)}: {MetricName}";
}
