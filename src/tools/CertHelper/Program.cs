using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CertHelper;

internal class Program
{
    static readonly string TENANT_ID = "72f988bf-86f1-41af-91ab-2d7cd011db47";
    static readonly string CERT_CLIENT_ID = "8c4b65ef-5a73-4d5a-a298-962d4a4ef7bc";
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
            var bcc = new BlobContainerClient(new Uri("https://pvscmdupload.blob.core.windows.net/certstatus"),
                new ClientCertificateCredential(TENANT_ID, CERT_CLIENT_ID, kvc.KeyVaultCertificates.First()));
            var currentKeyValutCertThumbprints = "";
            foreach(var cert in kvc.KeyVaultCertificates)
            {
                currentKeyValutCertThumbprints += $"[{DateTimeOffset.UtcNow}] {cert.Thumbprint}{Environment.NewLine}";
            }
            var blob = bcc.GetBlobClient(System.Environment.MachineName);
            if (blob.Exists())
            {
                var result = blob.DownloadContent();
                var currentBlob = result.Value.Content.ToString();
                currentBlob = currentBlob + currentKeyValutCertThumbprints;
                blob.Upload(new MemoryStream(Encoding.UTF8.GetBytes(currentBlob)), overwrite: true);
            }
            else
            {
                blob.Upload(new MemoryStream(Encoding.UTF8.GetBytes(currentKeyValutCertThumbprints)), overwrite: false);
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
