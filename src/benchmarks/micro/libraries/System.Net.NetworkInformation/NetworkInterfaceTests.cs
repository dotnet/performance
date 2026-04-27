// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.NetworkInformation;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Net.NetworkInformation.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class NetworkInterfaceTests
    {
        [Benchmark]
        public NetworkInterface[] GetAllNetworkInterfaces() => NetworkInterface.GetAllNetworkInterfaces();

        [Benchmark]
    #if NET5_0_OR_GREATER
        [System.Runtime.Versioning.UnsupportedOSPlatform("osx")]
        [System.Runtime.Versioning.UnsupportedOSPlatform("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatform("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatform("freebsd")]
    #endif
        public void GetAllNetworkInterfacesProperties()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceStatistics ips = ni.GetIPStatistics();
                IPInterfaceProperties ipp = ni.GetIPProperties();

                _ = ni.Id;
                _ = ni.Name;
                _ = ni.Description;
                _ = ni.NetworkInterfaceType;
                try { _ = ni.OperationalStatus; } catch { }
                try { _ = ni.Speed; } catch { }
                try { _ = ni.IsReceiveOnly; } catch { }
                try { _ = ni.SupportsMulticast; } catch { }

                // IP Statistics
                try { _ = ips.BytesReceived; } catch { }
                try { _ = ips.BytesSent; } catch { }
                try { _ = ips.IncomingPacketsDiscarded;} catch { }
                try { _ = ips.IncomingPacketsWithErrors;} catch { }
#if NET5_0_OR_GREATER
                if (!OperatingSystem.IsLinux())
#endif
                {
                    try { _ = ips.IncomingUnknownProtocolPackets;} catch { }
                    try { _ = ips.NonUnicastPacketsSent; } catch { }
                }
                try { _ = ips.NonUnicastPacketsReceived; } catch { }
                try { _ = ips.OutgoingPacketsDiscarded; } catch { }
                try { _ = ips.OutgoingPacketsWithErrors; } catch { }
                try { _ = ips.OutputQueueLength; } catch { }
                try { _ = ips.UnicastPacketsReceived; } catch { }
                try { _ = ips.UnicastPacketsSent; } catch { }

                // IP Properties
                try { _ = ipp.IsDnsEnabled; } catch { };
                try { _ = ipp.DnsSuffix; } catch { };
                try { _ = ipp.UnicastAddresses; } catch { };
                try { _ = ipp.MulticastAddresses; } catch { };
#if NET5_0_OR_GREATER
                if (OperatingSystem.IsWindows())
#endif
                {
                    try { _ = ipp.IsDynamicDnsEnabled; } catch { };
                    try { _ = ipp.AnycastAddresses; } catch { };
                }
                try { _ = ipp.DnsAddresses; } catch { };
                try { _ = ipp.GatewayAddresses; } catch { };
                try { _ = ipp.DhcpServerAddresses; } catch { };
                try { _ = ipp.DnsAddresses; } catch { };
            }
        }
    }
}
