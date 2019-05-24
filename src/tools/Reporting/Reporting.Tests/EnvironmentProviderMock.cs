// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Reporting.Tests
{
    internal class EnvironmentProviderMockBase : IEnvironment
    {
        public Dictionary<string, string> Vars { get; set; }
        public string GetEnvironmentVariable(string variable)
        {
            return Vars?[variable];
        }
    }
    internal class PerfLabEnvironmentProviderMock : EnvironmentProviderMockBase
    {
        public PerfLabEnvironmentProviderMock()
        {
            Vars = new Dictionary<string, string>()
            {
                {"PERFLAB_INLAB", "1" },
                {"HELIX_CORRELATION_ID","testCorrelationId" },
                {"PERFLAB_PERFHASH","testPerfHash" },
                {"PERFLAB_QUEUE","testQueue" },
                {"PERFLAB_REPO","testRepo" },
                {"PERFLAB_BRANCH","testBranch" },
                {"PERFLAB_BUILDARCH","testBuildArch" },
                {"PERFLAB_LOCALE","testLocale" },
                {"PERFLAB_HASH","testHash" },
                {"PERFLAB_BUILDNUM", "testBuildNum" },
                {"PERFLAB_HIDDEN", "false" },
                {"PERFLAB_RUNNAME", "testRunName" },
                {"PERFLAB_CONFIGS", "KEY1=VALUE1;KEY2=VALUE2" },
                {"PERFLAB_BUILDTIMESTAMP", new DateTime(1970,1,1).ToString("o") },
            };
        }

    }

    internal class NonPerfLabEnvironmentProviderMock : PerfLabEnvironmentProviderMock
    {
        public NonPerfLabEnvironmentProviderMock() : base()
        {
            var dict = new Dictionary<string, string>();
            foreach (var key in Vars.Keys)
                dict[key] = null;
            Vars = dict;
        }

    }
}