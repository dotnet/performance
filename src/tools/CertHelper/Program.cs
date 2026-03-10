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
        try
        {
            var kvc = new KeyVaultCert();
            await kvc.LoadKeyVaultCertsAsync();
            if (kvc.ShouldRotateCerts())
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    WriteCertsToDisk(kvc.KeyVaultCertificateBytes);
                }
                else
                {
                    using (var localMachineCerts = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                    {
                        localMachineCerts.Open(OpenFlags.ReadWrite);
                        localMachineCerts.RemoveRange(kvc.LocalCerts.Certificates);
                        localMachineCerts.AddRange(kvc.KeyVaultCertificates);
                    }
                }
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
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
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to rotate certificates");
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            ReadCertsFromDisk();
        }
        else
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite))
            {
                foreach(var cert in store.Certificates.Find(X509FindType.FindBySubjectName, "dotnetperf.microsoft.com", false))
                {
                    Console.WriteLine(Convert.ToBase64String(cert.Export(X509ContentType.Pfx)));
                }
            }
        }
        return 0;
    }

    static string GetMacCertDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "certs");
    }

    static void WriteCertsToDisk(List<byte[]> certBytes)
    {
        var certDir = GetMacCertDirectory();
        Directory.CreateDirectory(certDir);

        var certNames = new[] { Constants.Cert1Name, Constants.Cert2Name };
        for (int i = 0; i < certBytes.Count && i < certNames.Length; i++)
        {
            var pfxPath = Path.Combine(certDir, $"{certNames[i]}.pfx");
            File.WriteAllBytes(pfxPath, certBytes[i]);
            Console.Error.WriteLine($"Wrote certificate to {pfxPath}");
        }
    }

    static void ReadCertsFromDisk()
    {
        var certDir = GetMacCertDirectory();
        foreach (var certName in new[] { Constants.Cert1Name, Constants.Cert2Name })
        {
            var pfxPath = Path.Combine(certDir, $"{certName}.pfx");
            if (File.Exists(pfxPath))
            {
                Console.WriteLine(Convert.ToBase64String(File.ReadAllBytes(pfxPath)));
            }
            else
            {
                Console.Error.WriteLine($"Certificate file not found: {pfxPath}");
            }
        }
    }
}
