// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using Xunit;

namespace Tests
{
    public class CommandLineOptionsTests
    {
        [Fact]
        public void ArgsListContainsPartitionsCountAndIndex()
        {
            List<string> argsList = new List<string> {
                "--partition-count",
                "10",
                "--partition-index",
                "0"
            };

            int? count;
            int? index;
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-count", out count);
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-index", out index);

            CommandLineOptions.ValidatePartitionParameters(count, index);

            Assert.Equal(10, count);
            Assert.Equal(0, index);
        }
        [Fact]
        public void ArgsListContainsNeitherPartitionsCountAndIndex()
        {
            List<string> argsList = new List<string> {};

            int? count;
            int? index;
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-count", out count);
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-index", out index);

            CommandLineOptions.ValidatePartitionParameters(count, index);

            Assert.Null(count);
            Assert.Null(index);
        }
        [Fact]
        public void ArgsListContainsPartitionsCount()
        {
            List<string> argsList = new List<string> {
                "--partition-count",
                "10"
            };

            int? count;
            int? index;
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-count", out count);
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-index", out index);

            Assert.Throws<ArgumentException>(() => CommandLineOptions.ValidatePartitionParameters(count, index));
        }
        [Fact]
        public void ArgsListContainsPartitionsIndex()
        {
            List<string> argsList = new List<string> {
                "--partition-index",
                "10"
            };

            int? count;
            int? index;
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-count", out count);
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-index", out index);

            Assert.Throws<ArgumentException>(() => CommandLineOptions.ValidatePartitionParameters(count, index));
        }
        [Fact]
        public void BadPartitionCountValue()
        {
            List<string> argsList = new List<string> {
                "--partition-count",
                "0",
                "--partition-index",
                "0"
            };

            int? count;
            int? index;
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-count", out count);
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-index", out index);

            Assert.Throws<ArgumentException>(() => CommandLineOptions.ValidatePartitionParameters(count, index));
        }
        [Fact]
        public void BadPartitionIndexValue()
        {
            List<string> argsList = new List<string> {
                "--partition-count",
                "10",
                "--partition-index",
                "-1"
            };

            int? count;
            int? index;
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-count", out count);
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-index", out index);

            Assert.Throws<ArgumentException>(() => CommandLineOptions.ValidatePartitionParameters(count, index));
        }
        [Fact]
        public void PartitionIndexValueGreaterThanCount()
        {
            List<string> argsList = new List<string> {
                "--partition-count",
                "10",
                "--partition-index",
                "11"
            };

            int? count;
            int? index;
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-count", out count);
            CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-index", out index);

            Assert.Throws<ArgumentException>(() => CommandLineOptions.ValidatePartitionParameters(count, index));
        }
    }
}
