// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net.Test.Common
{
    public static class Configuration
    {
        public static class Certificates
        {
            private const string CertificatePassword = "testcertificate";
            private const string TestDataFolder = @"corefx\System.Net.Http";

            public static X509Certificate2 GetServerCertificate() => GetCertificate("testservereku.contoso.com.pfx");

            public static X509Certificate2 GetClientCertificate() => GetCertificate("testclienteku.contoso.com.pfx");

            private static X509Certificate2 GetCertificate(string certificateFileName) 
                => new X509Certificate2(
                    File.ReadAllBytes(Path.Combine(TestDataFolder, certificateFileName)),
                    CertificatePassword,
                    X509KeyStorageFlags.DefaultKeySet);
        }
    }
}
