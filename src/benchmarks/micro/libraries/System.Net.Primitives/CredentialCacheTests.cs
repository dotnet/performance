// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MicroBenchmarks;

namespace System.Net.Primitives.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class CredentialCacheTests
    {
        private const string UriPrefix = "http://name";
        private const string HostPrefix = "name";
        private const int Port = 80;
        private const string AuthenticationType = "authType";

        private static readonly NetworkCredential s_credential = new NetworkCredential();
        private Consumer _consumer = new Consumer();

        private readonly Dictionary<(int uriCount, int hostPortCount), CredentialCache> _caches =
            new Dictionary<(int uriCount, int hostPortCount), CredentialCache>
            {
                { (0, 0), CreateCredentialCache(0, 0) },
                { (0, 10), CreateCredentialCache(0, 10) },
                { (10, 0), CreateCredentialCache(10, 0) },
                { (10, 10), CreateCredentialCache(10, 10) }
        };

        [Benchmark]
        [Arguments("http://notfound", 0)]
        [Arguments("http://notfound", 10)]
        [Arguments("http://name5", 10)]
        public NetworkCredential GetCredential_Uri(string uriString, int uriCount)
        {
            CredentialCache cc = _caches[(uriCount: uriCount, hostPortCount: 0)];

            return cc.GetCredential(new Uri(uriString), AuthenticationType);
        }

        [Benchmark]
        [Arguments("notfound", 0)]
        [Arguments("notfound", 10)]
        [Arguments("name5", 10)]
        public NetworkCredential GetCredential_HostPort(string host, int hostPortCount)
        {
            CredentialCache cc = _caches[(uriCount: 0, hostPortCount: hostPortCount)];

            return cc.GetCredential(host, Port, AuthenticationType);
        }

        [Benchmark]
        [Arguments(0, 0)]
        [Arguments(10, 0)]
        [Arguments(0, 10)]
        [Arguments(10, 10)]
        public void ForEach(int uriCount, int hostPortCount)
        {
            CredentialCache cc = _caches[(uriCount: uriCount, hostPortCount: hostPortCount)];
            Consumer consumer = _consumer;
            
            foreach (var c in cc)
            {
                consumer.Consume(c);
            }
        }

        private static CredentialCache CreateCredentialCache(int uriCount, int hostPortCount)
        {
            var cc = new CredentialCache();

            for (int i = 0; i < uriCount; i++)
            {
                Uri uri = new Uri(UriPrefix + i.ToString());
                cc.Add(uri, AuthenticationType, s_credential);
            }

            for (int i = 0; i < hostPortCount; i++)
            {
                string host = HostPrefix + i.ToString();
                cc.Add(host, Port, AuthenticationType, s_credential);
            }

            return cc;
        }
    }
}
