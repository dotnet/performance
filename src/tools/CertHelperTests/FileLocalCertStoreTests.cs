using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace CertHelper.Tests;

public class FileLocalCertStoreTests : IDisposable
{
    private readonly string _directory;

    public FileLocalCertStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "certhelper-tests-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static X509Certificate2 MakeCert(string subject = "CN=dotnetperf.microsoft.com")
    {
        using var rsa = RSA.Create();
        var req = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
    }

    [Fact]
    public void GetCertificates_ShouldReturnEmpty_WhenDirectoryMissing()
    {
        var store = new FileLocalCertStore(_directory);

        var result = store.GetCertificates();

        Assert.Empty(result);
    }

    [Fact]
    public void Rotate_ShouldWriteCertificates_ThatCanBeReloaded()
    {
        var store = new FileLocalCertStore(_directory);
        var cert1 = MakeCert();
        var cert2 = MakeCert();
        var toAdd = new X509Certificate2Collection { cert1, cert2 };

        store.Rotate(new X509Certificate2Collection(), toAdd);

        var reloaded = store.GetCertificates();
        Assert.Equal(2, reloaded.Count);
        var thumbprints = reloaded.Cast<X509Certificate2>().Select(c => c.Thumbprint).ToHashSet();
        Assert.Contains(cert1.Thumbprint, thumbprints);
        Assert.Contains(cert2.Thumbprint, thumbprints);
    }

    [Fact]
    public void Rotate_ShouldReplaceExistingCertificates()
    {
        var store = new FileLocalCertStore(_directory);
        var oldCert = MakeCert();
        store.Rotate(new X509Certificate2Collection(), new X509Certificate2Collection { oldCert });

        var newCert = MakeCert();
        store.Rotate(new X509Certificate2Collection { oldCert }, new X509Certificate2Collection { newCert });

        var reloaded = store.GetCertificates();
        Assert.Single(reloaded);
        Assert.Equal(newCert.Thumbprint, reloaded[0].Thumbprint);
    }

    [Fact]
    public void Rotate_ReloadedCertificate_ShouldBeExportableWithPrivateKey()
    {
        var store = new FileLocalCertStore(_directory);
        var cert = MakeCert();

        store.Rotate(new X509Certificate2Collection(), new X509Certificate2Collection { cert });

        var reloaded = store.GetCertificates();
        Assert.Single(reloaded);
        Assert.True(reloaded[0].HasPrivateKey);
        // Must be exportable so Program.Main can emit the PFX to stdout without an OS keystore round-trip.
        var exported = reloaded[0].Export(X509ContentType.Pfx);
        Assert.NotEmpty(exported);
    }
}
