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

        [Fact]
        public void ParseAndRemoveStringsParameterCollectsValuesUntilNextFlag()
        {
            List<string> argsList = new List<string> {
                "--exclusion-filter",
                "System.*",
                "Microsoft.*",
                "--partition-count",
                "4"
            };

            var remaining = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--exclusion-filter", out List<string> filters);

            Assert.Equal(new[] { "System.*", "Microsoft.*" }, filters);
            Assert.Equal(new[] { "--partition-count", "4" }, remaining);
        }

        [Fact]
        public void ParseAndRemoveBooleanParameterRemovesSwitchWhenPresent()
        {
            List<string> argsList = new List<string> {
                "--wasm",
                "--filter",
                "*"
            };

            CommandLineOptions.ParseAndRemoveBooleanParameter(argsList, "--wasm", out bool enabled);

            Assert.True(enabled);
            Assert.Equal(new[] { "--filter", "*" }, argsList);
        }

        [Fact]
        public void ParseAndRemoveStringsParameterLeavesArgsUntouchedWhenSwitchIsMissing()
        {
            List<string> argsList = new List<string> {
                "literal-value",
                "--filter",
                "*"
            };

            var remaining = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--exclusion-filter", out List<string> filters);

            Assert.Empty(filters);
            Assert.Equal(new[] { "literal-value", "--filter", "*" }, remaining);
        }

        [Fact]
        public void ParseAndRemoveBooleanParameterReturnsFalseWhenSwitchIsMissing()
        {
            List<string> argsList = new List<string> {
                "--filter",
                "*"
            };

            CommandLineOptions.ParseAndRemoveBooleanParameter(argsList, "--wasm", out bool enabled);

            Assert.False(enabled);
            Assert.Equal(new[] { "--filter", "*" }, argsList);
        }

        [Theory]
        [InlineData("--partition-count")]
        [InlineData("--partition-index")]
        public void ParseAndRemoveIntParameterThrowsWhenValueIsMissing(string parameter)
        {
            List<string> argsList = new List<string> { parameter };

            Assert.Throws<ArgumentException>(() => CommandLineOptions.ParseAndRemoveIntParameter(argsList, parameter, out int? _));
        }

        [Theory]
        [InlineData("--partition-count", "abc")]
        [InlineData("--partition-index", "3.14")]
        public void ParseAndRemoveIntParameterThrowsWhenValueIsNotAnInteger(string parameter, string value)
        {
            List<string> argsList = new List<string> {
                parameter,
                value
            };

            Assert.Throws<ArgumentException>(() => CommandLineOptions.ParseAndRemoveIntParameter(argsList, parameter, out int? _));
        }
    }
}
