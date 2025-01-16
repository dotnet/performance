using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CertHelper;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var kvc = new KeyVaultCert();
            await kvc.GetKeyVaultCerts();
            if (kvc.ShouldRotateCerts())
            {
                using (var localMachineCerts = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    localMachineCerts.Open(OpenFlags.ReadWrite);
                    localMachineCerts.RemoveRange(kvc.LocalCerts.Certificates);
                    localMachineCerts.AddRange(kvc.KeyVaultCertificates);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to rotate certificates");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }

        using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite))
        {
            foreach(var cert in store.Certificates.Find(X509FindType.FindBySubjectName, "dotnetperf.microsoft.com", false))
            {
                Console.WriteLine(Convert.ToBase64String(cert.Export(X509ContentType.Pfx)));
            }
        }
        return 0;
    }
}
