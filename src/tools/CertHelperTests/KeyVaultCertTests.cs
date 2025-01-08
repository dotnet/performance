using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace CertHelper.Tests;

public class KeyVaultCertTests
{
    [Fact]
    public async Task GetKeyVaultCerts_ShouldAddCertificatesToCollection()
    {
        // Arrange
        Mock<TokenCredential> mockTokenCred;
        Mock<CertificateClient> mockCertClient;
        Mock<SecretClient> mockSecretClient;
        Mock<ILocalCert> mockLocalCert;
        CertStoreSetup(out mockTokenCred, out mockCertClient, out mockSecretClient, out mockLocalCert, false);

        var keyVaultCert = new KeyVaultCert(mockTokenCred.Object, mockCertClient.Object, mockSecretClient.Object, mockLocalCert.Object);

        // Act
        await keyVaultCert.GetKeyVaultCerts();

        // Assert
        Assert.Equal(2, keyVaultCert.KeyVaultCertificates.Count);
    }

    private static void CertStoreSetup(out Mock<TokenCredential> mockTokenCred, out Mock<CertificateClient> mockCertClient, out Mock<SecretClient> mockSecretClient, out Mock<ILocalCert> mockLocalCert, bool missingKeyVaultCerts = false, bool localAndKeyVaultDifferent = false)
    {
        mockTokenCred = new Mock<TokenCredential>();
        mockTokenCred.Setup(tc => tc.GetTokenAsync(It.IsAny<TokenRequestContext>(), default)).ReturnsAsync(new AccessToken("token", DateTimeOffset.Now));

        mockCertClient = new Mock<CertificateClient>(new Uri("https://dotnetperfkeyvault.vault.azure.net/"), mockTokenCred.Object);
        mockSecretClient = new Mock<SecretClient>(new Uri("https://dotnetperfkeyvault.vault.azure.net/"), mockTokenCred.Object);
        KeyVaultCertificateWithPolicy? mockCert1, mockCert2;
        X509Certificate2 cert1, cert2;
        MakeCerts(out mockCert1, out mockCert2, out cert1, out cert2, localAndKeyVaultDifferent);

        var certCollection = new X509Certificate2Collection { cert1, cert2 };

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        mockCertClient.Setup(c => c.GetCertificateAsync(Constants.Cert1Name, default)).ReturnsAsync(Response.FromValue(mockCert1, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        if (missingKeyVaultCerts)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            mockCertClient.Setup(c => c.GetCertificateAsync(Constants.Cert2Name, default)).ReturnsAsync(Response.FromValue<KeyVaultCertificateWithPolicy>(null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        else
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            mockCertClient.Setup(c => c.GetCertificateAsync(Constants.Cert2Name, default)).ReturnsAsync(Response.FromValue(mockCert2, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        //var secret1 = new KeyVaultSecret(Constants.Cert1Name, Convert.ToBase64String(certCollection[0].Export(X509ContentType.Cert)));
        //var secret2 = new KeyVaultSecret(Constants.Cert2Name, Convert.ToBase64String(certCollection[1].Export(X509ContentType.Cert)));
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var secret1 = new KeyVaultSecret(Constants.Cert1Name, Convert.ToBase64String(mockCert1.Cer));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var secret2 = new KeyVaultSecret(Constants.Cert1Name, Convert.ToBase64String(mockCert2.Cer));
#pragma warning restore CS8602 // Dereference of a possibly null reference.

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        mockSecretClient.Setup(s => s.GetSecretAsync(Constants.Cert1Name, mockCert1.SecretId.Segments.Last(), default)).ReturnsAsync(Response.FromValue(secret1, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        mockSecretClient.Setup(s => s.GetSecretAsync(Constants.Cert2Name, mockCert2.SecretId.Segments.Last(), default)).ReturnsAsync(Response.FromValue(secret2, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        mockLocalCert = new Mock<ILocalCert>();
        mockLocalCert.Setup(lc => lc.Certificates).Returns(certCollection);
    }

    private static void MakeCerts(out KeyVaultCertificateWithPolicy? mockCert1, out KeyVaultCertificateWithPolicy? mockCert2, out X509Certificate2 cert1, out X509Certificate2 cert2, bool localAndKeyVaultDifferent = false)
    {
        var ecdsa1 = ECDsa.Create(); // generate asymmetric key pair
        var req1 = new CertificateRequest("cn=perflabtest", ecdsa1, HashAlgorithmName.SHA256);
        var tmpCert1 = req1.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        var ecdsa2 = ECDsa.Create(); // generate asymmetric key pair
        var req2 = new CertificateRequest("cn=perflabtest", ecdsa2, HashAlgorithmName.SHA256);
        var tmpCert2 = req2.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        cert1 = tmpCert1;
        cert2 = tmpCert2;

        if(localAndKeyVaultDifferent)
        {
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest("cn=perflabtest", ecdsa, HashAlgorithmName.SHA256);
            tmpCert1 = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        }

        mockCert1 = CertificateModelFactory.KeyVaultCertificateWithPolicy(CertificateModelFactory.CertificateProperties(Constants.Cert1Id, Constants.Cert1Name, x509thumbprint: Convert.FromHexString(tmpCert1.Thumbprint)),
            Constants.Cert1Id, Constants.Cert1Id, tmpCert1.GetRawCertData());
        mockCert2 = CertificateModelFactory.KeyVaultCertificateWithPolicy(CertificateModelFactory.CertificateProperties(Constants.Cert2Id, Constants.Cert2Name, x509thumbprint: Convert.FromHexString(tmpCert2.Thumbprint)),
            Constants.Cert2Id, Constants.Cert2Id, tmpCert2.GetRawCertData());
    }

    [Fact]
    public async Task GetKeyVaultCerts_ShouldThrowException_WhenCertificatesNotFound()
    {
        // Arrange
        Mock<TokenCredential> mockTokenCred;
        Mock<CertificateClient> mockCertClient;
        Mock<SecretClient> mockSecretClient;
        Mock<ILocalCert> mockLocalCert;
        CertStoreSetup(out mockTokenCred, out mockCertClient, out mockSecretClient, out mockLocalCert, missingKeyVaultCerts: true);

        var keyVaultCert = new KeyVaultCert(mockTokenCred.Object, mockCertClient.Object, mockSecretClient.Object, mockLocalCert.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => keyVaultCert.GetKeyVaultCerts());
    }

    [Fact]
    public async Task ShouldRotateCerts_ShouldReturnTrue_WhenThumbprintsDoNotMatch()
    {
        // Arrange
        Mock<TokenCredential> mockTokenCred;
        Mock<CertificateClient> mockCertClient;
        Mock<SecretClient> mockSecretClient;
        Mock<ILocalCert> mockLocalCert;
        CertStoreSetup(out mockTokenCred, out mockCertClient, out mockSecretClient, out mockLocalCert, localAndKeyVaultDifferent: true);

        var keyVaultCert = new KeyVaultCert(mockTokenCred.Object, mockCertClient.Object, mockSecretClient.Object, mockLocalCert.Object);

        // Act
        await keyVaultCert.GetKeyVaultCerts();

        var result = keyVaultCert.ShouldRotateCerts();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ShouldRotateCerts_ShouldReturnFalse_WhenThumbprintsMatch()
    {
        // Arrange
        Mock<TokenCredential> mockTokenCred;
        Mock<CertificateClient> mockCertClient;
        Mock<SecretClient> mockSecretClient;
        Mock<ILocalCert> mockLocalCert;
        CertStoreSetup(out mockTokenCred, out mockCertClient, out mockSecretClient, out mockLocalCert);

        var keyVaultCert = new KeyVaultCert(mockTokenCred.Object, mockCertClient.Object, mockSecretClient.Object, mockLocalCert.Object);

        // Act
        await keyVaultCert.GetKeyVaultCerts();

        var result = keyVaultCert.ShouldRotateCerts();

        // Assert
        Assert.False(result);
    }
}

