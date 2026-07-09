using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CertHelper;

internal class Program
{

    static readonly string TENANT_ID = "72f988bf-86f1-41af-91ab-2d7cd011db47";
    static readonly string CERT_CLIENT_ID = "8c4b65ef-5a73-4d5a-a298-962d4a4ef7bc";

    static async Task<int> Main(string[] args)
    {
        var certStore = LocalCertStoreFactory.Create();
        X509Certificate2Collection? certsToExport = null;
        try
        {
            var localCerts = new LocalCert(certStore);
            var kvc = new KeyVaultCert(localCerts: localCerts);
            await kvc.LoadKeyVaultCertsAsync();
            if (kvc.ShouldRotateCerts())
            {
                certStore.Rotate(kvc.LocalCerts.Certificates, kvc.KeyVaultCertificates);
            }
            certsToExport = kvc.KeyVaultCertificates;
            var bcc = new BlobContainerClient(new Uri("https://pvscmdupload.blob.core.windows.net/certstatus"),
                new ClientCertificateCredential(TENANT_ID, CERT_CLIENT_ID, kvc.KeyVaultCertificates.First(), new() {SendCertificateChain = true}));
            var currentKeyValutCertThumbprints = "";
            foreach (var cert in kvc.KeyVaultCertificates)
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
            Console.Error.WriteLine("Failed to rotate certificates");
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
        }

        // Export the current certificates to stdout. Prefer the in-memory Key Vault certificates
        // (loaded as exportable) so we never round-trip through the OS store, which cannot export
        // private keys on macOS. If Key Vault auth failed above, fall back to whatever is currently
        // persisted locally so callers can still attempt to use existing certificates.
        var exportSource = certsToExport ?? certStore.GetCertificates();
        foreach (var cert in exportSource.Find(X509FindType.FindBySubjectName, "dotnetperf.microsoft.com", false))
        {
            Console.WriteLine(Convert.ToBase64String(cert.Export(X509ContentType.Pfx)));
        }
        return 0;
    }
}
