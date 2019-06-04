// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public partial class Utf8JsonReaderTests
    {
        private const int Iterations = 1_000_000;

        private byte[] s_singleEmptyMultiLineComment;
        private byte[] s_multipleEmptyMultiLineComment;
        private byte[] s_singleShortMultiLineComment;
        private byte[] s_multipleShortMultiLineComment;
        private byte[] s_singleLongMultiLineComment1;
        private byte[] s_singleLongMultiLineComment2;

        private byte[] s_singleEmptySingleLineComment;
        private byte[] s_multipleEmptySingleLineComment;
        private byte[] s_singleShortSingleLineComment;
        private byte[] s_multipleShortSingleLineComment;
        private byte[] s_singleLongSingleLineComment;

        [GlobalSetup]
        public void Setup()
        {
            // every test case ends with {} so that we can re-use it for both Skip and Allow options
            s_singleEmptyMultiLineComment = Encoding.UTF8.GetBytes("/**/{}");
            s_multipleEmptyMultiLineComment = Encoding.UTF8.GetBytes("/**//**//**//**//**//**//**//**//**//**//**//**//**//**//**//**//**//**/{}");
            s_singleShortMultiLineComment = Encoding.UTF8.GetBytes("/*asdasd*/{}");
            s_multipleShortMultiLineComment = Encoding.UTF8.GetBytes("/*asdasd*//*asdasd*//*asdasd*//*asdasd*//*asdasd*//*asdasd*//*asdasd*//*asdasd*//*asdasd*//*asdasd*/{}");
            s_singleLongMultiLineComment1 = Encoding.UTF8.GetBytes("/*" + new string('c', 2000) + "*/{}");
            s_singleLongMultiLineComment2 = Encoding.UTF8.GetBytes("/*" + new string('\n', 2000) + "*/{}");

            s_singleEmptySingleLineComment = Encoding.UTF8.GetBytes("//\n{}");
            s_multipleEmptySingleLineComment = Encoding.UTF8.GetBytes("//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n//\n{}");
            s_singleShortSingleLineComment = Encoding.UTF8.GetBytes("//asdasd\n{}");
            s_multipleShortSingleLineComment = Encoding.UTF8.GetBytes("//asdasd\n//asdasd\n//asdasd\n//asdasd\n//asdasd\n//asdasd\n//asdasd\n//asdasd\n//asdasd\n//asdasd\n//asdasd\n//asdasd\n//asdasd\n{}");
            s_singleLongSingleLineComment = Encoding.UTF8.GetBytes("//" + new string('c', 2000) + "\n{}");

            MultiSegmentSetup();
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleEmptyMultiLineComment_CommentHandling_Allow() => GenericReadTest(s_singleEmptyMultiLineComment, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultipleEmptyMultiLineComment_CommentHandling_Allow() => GenericReadTest(s_multipleEmptyMultiLineComment, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleShortMultiLineComment_CommentHandling_Allow() => GenericReadTest(s_singleShortMultiLineComment, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultipleShortMultiLineComment_CommentHandling_Allow() => GenericReadTest(s_multipleShortMultiLineComment, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleLongMultiLineComment1_CommentHandling_Allow() => GenericReadTest(s_singleLongMultiLineComment1, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleLongMultiLineComment2_CommentHandling_Allow() => GenericReadTest(s_singleLongMultiLineComment2, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleEmptySingleLineComment_CommentHandling_Allow() => GenericReadTest(s_singleEmptySingleLineComment, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultipleEmptySingleLineComment_CommentHandling_Allow() => GenericReadTest(s_multipleEmptySingleLineComment, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleShortSingleLineComment_CommentHandling_Allow() => GenericReadTest(s_singleShortSingleLineComment, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultipleShortSingleLineComment_CommentHandling_Allow() => GenericReadTest(s_multipleShortSingleLineComment, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleLongSingleLineComment_CommentHandling_Allow() => GenericReadTest(s_singleLongSingleLineComment, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleEmptyMultiLineComment_CommentHandling_Skip() => GenericReadTest(s_singleEmptyMultiLineComment, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultipleEmptyMultiLineComment_CommentHandling_Skip() => GenericReadTest(s_multipleEmptyMultiLineComment, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleShortMultiLineComment_CommentHandling_Skip() => GenericReadTest(s_singleShortMultiLineComment, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultipleShortMultiLineComment_CommentHandling_Skip() => GenericReadTest(s_multipleShortMultiLineComment, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleLongMultiLineComment1_CommentHandling_Skip() => GenericReadTest(s_singleLongMultiLineComment1, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleLongMultiLineComment2_CommentHandling_Skip() => GenericReadTest(s_singleLongMultiLineComment2, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleEmptySingleLineComment_CommentHandling_Skip() => GenericReadTest(s_singleEmptySingleLineComment, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultipleEmptySingleLineComment_CommentHandling_Skip() => GenericReadTest(s_multipleEmptySingleLineComment, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleShortSingleLineComment_CommentHandling_Skip() => GenericReadTest(s_singleShortSingleLineComment, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultipleShortSingleLineComment_CommentHandling_Skip() => GenericReadTest(s_multipleShortSingleLineComment, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void SingleLongSingleLineComment_CommentHandling_Skip() => GenericReadTest(s_singleLongSingleLineComment, JsonCommentHandling.Skip);

        private static void GenericReadTest(byte[] dataUtf8, JsonCommentHandling commentHandling)
        {
            for (int i = 0; i < Iterations; i++)
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

                while (json.Read())
                {
                }
            }
        }
    }
}