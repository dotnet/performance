// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    public class JsonNumberSet
    {
        private readonly string description;
        public readonly byte[] int32Json;
        public readonly byte[] int64Json;
        public readonly byte[] floatJson;
        public readonly byte[] doubleJson;
        public readonly byte[] decimalJson;

        public JsonNumberSet(string description, string int32Json, string int64Json, string floatJson, string doubleJson, string decimalJson)
        {
            this.description = description;
            this.int32Json = PrepareJson(int32Json);
            this.int64Json = PrepareJson(int64Json);
            this.floatJson = PrepareJson(floatJson);
            this.doubleJson = PrepareJson(doubleJson);
            this.decimalJson = PrepareJson(decimalJson);
        }

        public static byte[] PrepareJson(string value) => Encoding.UTF8.GetBytes("{\"id\": " + value + "}");

        public override string ToString() => description;
    }
}
