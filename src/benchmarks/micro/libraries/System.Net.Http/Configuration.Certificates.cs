// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Test.Common
{
    public static class Configuration
    {
        public static class Certificates
        {
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Test certificate password.")]
            private const string CertificatePassword = "testcertificate";

            public static X509Certificate2 GetServerCertificate() => GetCertificate("testservereku.contoso.com.pfx");

            public static X509Certificate2 GetClientCertificate() => GetCertificate("testclienteku.contoso.com.pfx");

            public static X509Certificate2 GetEC256Certificate() => GetCertificate("ec256.pfx");
            public static X509Certificate2 GetEC512Certificate() => GetCertificate("ec512.pfx");
            public static X509Certificate2 GetRSA2048Certificate() => GetCertificate("rsa2048.pfx");
            public static X509Certificate2 GetRSA4096Certificate() => GetCertificate("rsa4096.pfx");

            private static X509Certificate2 GetCertificate(string certificateFileName)
            {
                try
                {
#pragma warning disable SYSLIB0057 // Type or member is obsolete
                    return new X509Certificate2(
                         File.ReadAllBytes(
                             Path.Combine(
                                 AppContext.BaseDirectory,
                                 "libraries",
                                 "System.Net.Http",
                                 certificateFileName)),
                         CertificatePassword,
                         X509KeyStorageFlags.DefaultKeySet);
                }
#pragma warning restore SYSLIB0057
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
