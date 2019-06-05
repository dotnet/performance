// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace System.Text.Json.Tests
{
    //using static Utf8JsonReaderCommentsTests.TestCaseType;

    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public partial class Utf8JsonReaderCommentsTests
    {
        [Params(JsonCommentHandling.Skip, JsonCommentHandling.Allow)]
        public JsonCommentHandling CommentHandling;

        // 0 => single segment
        [Params(0, 1, 100)]
        public int SegmentSize;

        private bool MultiSegment => SegmentSize != 0;
        private byte[] _jsonPayload;
        private ReadOnlySequence<byte> _jsonPayloadSequence;

        [ParamsSource(nameof(TestCaseValues))]
        public TestCaseType TestCase;

        public static IEnumerable<TestCaseType> TestCaseValues() => (IEnumerable<TestCaseType>)Enum.GetValues(typeof(TestCaseType));

        private static Dictionary<TestCaseType, string> s_testCases = new Dictionary<TestCaseType, string>()
        {
            { TestCaseType.SingleEmptyMultiLineComment, "{}/**/" },
            { TestCaseType.MultipleEmptyMultiLineComment, "{}/**//**//**//**//**//**//**//**//**//**//**//**//**//**//**//**//**//**/" },
            { TestCaseType.SingleShortMultiLineComment, "{}/*asdasd*/" },
            { TestCaseType.MultipleShortMultiLineComment, "[1, /*asdasd*/ 2, /*asdasd*/ 3, /*asdasd*/ 4, /*asdasd*/ 5, /*asdasd*/ 6, /*asdasd*/ 7,/*asdasd*/ 8, /*asdasd*/ 9, /*asdasd*/ 10 /*asdasd*/]" },
            { TestCaseType.SingleLongMultiLineComment1, "{}/*" + new string('c', 2000) + "*/" },
            { TestCaseType.SingleLongMultiLineComment2, "{}/*" + new string('\n', 2000) + "*/" },

            { TestCaseType.SingleEmptySingleLineComment, "{}//\n" },
            { TestCaseType.MultipleEmptySingleLineComment, "{}//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n" },
            { TestCaseType.SingleShortSingleLineComment, "{}//asdasd\n" },
            { TestCaseType.MultipleShortSingleLineComment, "[1, //asdasd\n2, //asdasd\n3, //asdasd\n4, //asdasd\n5, //asdasd\n6, //asdasd\n7, //asdasd\n8, //asdasd\n9, //asdasd\n10, //asdasd\n11, //asdasd\n12, //asdasd\n13 //asdasd\n]" },
            { TestCaseType.SingleLongSingleLineComment, "{}//" + new string('c', 2000) + "\n" },
        };

        [GlobalSetup]
        public void Setup()
        {
            _jsonPayload = Encoding.UTF8.GetBytes(s_testCases[TestCase]);

            if (MultiSegment)
            {
                _jsonPayloadSequence = GetSequence(_jsonPayload, SegmentSize);
            }
        }

        [Benchmark]
        public void Utf8JsonReaderCommentParsing()
        {
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = CommentHandling });
            Utf8JsonReader reader = MultiSegment ?
                new Utf8JsonReader(_jsonPayloadSequence, isFinalBlock: true, state) :
                new Utf8JsonReader(_jsonPayload, isFinalBlock: true, state);

            while (reader.Read())
            {
            }
        }

        public enum TestCaseType
        {
            SingleEmptyMultiLineComment,
            MultipleEmptyMultiLineComment,
            SingleShortMultiLineComment,
            MultipleShortMultiLineComment,
            SingleLongMultiLineComment1,
            SingleLongMultiLineComment2,
            SingleEmptySingleLineComment,
            MultipleEmptySingleLineComment,
            SingleShortSingleLineComment,
            MultipleShortSingleLineComment,
            SingleLongSingleLineComment,
        }
    }
}