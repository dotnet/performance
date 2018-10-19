﻿using System;
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
