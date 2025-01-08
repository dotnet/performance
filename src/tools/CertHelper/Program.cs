using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CertHelper;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        var kvc = new KeyVaultCert();
        await kvc.GetKeyVaultCerts();
        var updated = false;
        if (kvc.ShouldRotateCerts())
        {
            using (var localMachineCerts = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                try
                {
                    localMachineCerts.Open(OpenFlags.ReadWrite);
                }
                catch (System.Security.Cryptography.CryptographicException ex) when (ex.Message.Contains("Unix"))
                {
                    var localMachineCertsCurrentUser = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                    localMachineCertsCurrentUser.Open(OpenFlags.ReadWrite);
                    localMachineCertsCurrentUser.RemoveRange(kvc.LocalCerts.Certificates);
                    localMachineCertsCurrentUser.AddRange(kvc.KeyVaultCertificates);
                    localMachineCertsCurrentUser.Close();
                    updated = true;
                }
                if(!updated)
                {
                    localMachineCerts.RemoveRange(kvc.LocalCerts.Certificates);
                    localMachineCerts.AddRange(kvc.KeyVaultCertificates);
                }
            }
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
