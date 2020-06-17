// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using ProtoBuf;

namespace MicroBenchmarks.Serializers.Helpers
{
    [ProtoContract]
    public class DateTimeOffsetSurrogate
    {
        [ProtoMember(1)]
        public string DateTimeString { get; set; }

        public static implicit operator DateTimeOffsetSurrogate(DateTimeOffset value)
        {
            return new DateTimeOffsetSurrogate { DateTimeString = value.ToString("u") };
        }

        public static implicit operator DateTimeOffset(DateTimeOffsetSurrogate value)
        {
            return DateTimeOffset.Parse(value.DateTimeString);
        }
    }
}
