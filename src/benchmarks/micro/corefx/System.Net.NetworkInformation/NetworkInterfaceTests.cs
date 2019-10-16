// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Net.NetworkInformation.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class NetworkInterfaceTests
    {
        private PropertyInfo[] NetworkInterfaceInfo = typeof(System.Net.NetworkInformation.NetworkInterface).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        private PropertyInfo[] IPInterfaceStatisticsInfo = typeof(System.Net.NetworkInformation.IPInterfaceStatistics).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        private PropertyInfo[] IPInterfacePropertiesInfo = typeof(System.Net.NetworkInformation.IPInterfaceProperties).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        [Benchmark]
        public void GetAllNetworkInterfaces() => NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

        [Benchmark]
        public void GetAllNetworkInterfacesProperties()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceStatistics ips = ni.GetIPStatistics();
                IPInterfaceProperties ipp = ni.GetIPProperties();

                foreach(var pi in NetworkInterfaceInfo)
                {
                    try
                    {
                        _ = pi.GetValue(ni);
                    }
                    catch (Exception) { };
                }

                foreach(var pi in IPInterfaceStatisticsInfo)
                {
                    try
                    {
                        _ = pi.GetValue(ips);
                    }
                    catch (Exception) { };
                }

                foreach(var pi in IPInterfacePropertiesInfo)
                {
                    try
                    {
                        _ = pi.GetValue(ipp);
                    }
                    catch (Exception) { };
                }
            }
        }
    }
}
