using Azure.Security.KeyVault.Certificates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CertHelper;

public class LocalCert : ILocalCert
{
    public X509Certificate2Collection Certificates { get; set; }
    internal IX509Store LocalMachineCerts { get; set; }

    public LocalCert(IX509Store? store = null)
    {
        LocalMachineCerts = store ?? new TestableX509Store();
        Certificates = new X509Certificate2Collection();
        GetLocalCerts();
    }

    private void GetLocalCerts()
    {
        foreach (var cert in LocalMachineCerts.Certificates.Find(X509FindType.FindBySubjectName, "dotnetperf.microsoft.com", false))
        {
            if (cert.Subject == "CN=dotnetperf.microsoft.com")
            {
                Certificates.Add(cert);
            }
        }

        if (Certificates.Count < 2 || Certificates.Where(c => c == null).Count() > 0)
        {
            throw new Exception("One or more certificates not found");
        }
    }
}

public interface ILocalCert
{
    X509Certificate2Collection Certificates { get; set; }
}
