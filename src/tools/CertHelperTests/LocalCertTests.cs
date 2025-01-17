using System.Security.Cryptography.X509Certificates;
using Xunit;
using Moq;
using Microsoft.VisualBasic;
using System.Security.Cryptography;

namespace CertHelper.Tests;

public class LocalCertTests
{
    [Fact]
    public void GetLocalCerts_ShouldAddCertificatesToCollection()
    {
        // Arrange
        var mockStore = new Mock<IX509Store>();
        var ecdsa1 = ECDsa.Create(); // generate asymmetric key pair
        var req1 = new CertificateRequest("CN=dotnetperf.microsoft.com", ecdsa1, HashAlgorithmName.SHA256);
        var cert1 = req1.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        var ecdsa2 = ECDsa.Create(); // generate asymmetric key pair
        var req2 = new CertificateRequest("CN=dotnetperf.microsoft.com", ecdsa2, HashAlgorithmName.SHA256);
        var cert2 = req2.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

        var mockCert1 = cert1;
        var mockCert2 = cert2;

        var certCollection = new X509Certificate2Collection { mockCert1, mockCert2 };
        mockStore.Setup(s => s.Certificates).Returns(certCollection);



        // Act
        var localCert = new LocalCert(mockStore.Object);

        // Assert
        Assert.Equal(2, localCert.Certificates.Count);
        Assert.Contains(mockCert1, localCert.Certificates);
        Assert.Contains(mockCert2, localCert.Certificates);
    }

    [Fact]
    public void GetLocalCerts_ShouldAddCertificatesToCollection_WhenSubjectMatches()
    {
        // Arrange
        var mockStore = new Mock<IX509Store>();
        var ecdsa1 = ECDsa.Create(); // generate asymmetric key pair
        var req1 = new CertificateRequest("CN=dotnetperf.microsoft.com", ecdsa1, HashAlgorithmName.SHA256);
        var cert1 = req1.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        var ecdsa2 = ECDsa.Create(); // generate asymmetric key pair
        var req2 = new CertificateRequest("CN=dotnetperf.microsoft.com", ecdsa2, HashAlgorithmName.SHA256);
        var cert2 = req2.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

        var mockCert1 = cert1;
        var mockCert2 = cert2;

        var certCollection = new X509Certificate2Collection { mockCert1, mockCert2 };
        mockStore.Setup(s => s.Certificates).Returns(certCollection);

        // Act
        var localCert = new LocalCert(mockStore.Object);

        // Assert
        Assert.Equal(2, localCert.Certificates.Count);
        Assert.Contains(mockCert1, localCert.Certificates);
        Assert.Contains(mockCert2, localCert.Certificates);
    }

    [Fact]
    public void GetLocalCerts_ShouldThrowException_WhenOneCertificateFound()
    {
        // Arrange
        var mockStore = new Mock<IX509Store>();
        var ecdsa1 = ECDsa.Create(); // generate asymmetric key pair
        var req1 = new CertificateRequest("CN=dotnetperf.microsoft.com", ecdsa1, HashAlgorithmName.SHA256);
        var cert1 = req1.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        var certCollection = new X509Certificate2Collection { cert1 };
        mockStore.Setup(s => s.Certificates).Returns(certCollection);

        // Act & Assert
        Assert.Throws<Exception>(() => new LocalCert(mockStore.Object));
    }

    [Fact]
    public void GetLocalCerts_ShouldThrowException_WhenCertificatesHaveWrongSubject()
    {
        // Arrange
        var mockStore = new Mock<IX509Store>();
        var ecdsa1 = ECDsa.Create(); // generate asymmetric key pair
        var req1 = new CertificateRequest("CN=dotnetperf.microsoft.co", ecdsa1, HashAlgorithmName.SHA256);
        var cert1 = req1.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        var ecdsa2 = ECDsa.Create(); // generate asymmetric key pair
        var req2 = new CertificateRequest("CN=dotnetperf.microsoft.co", ecdsa1, HashAlgorithmName.SHA256);
        var cert2 = req1.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

        var certCollection = new X509Certificate2Collection { cert1, cert2 };
        mockStore.Setup(s => s.Certificates).Returns(certCollection);

        // Act & Assert
        Assert.Throws<Exception>(() => new LocalCert(mockStore.Object));
    }

    [Fact]
    public void GetLocalCerts_ShouldThrowException_WhenCertificatesNotFound()
    {
        // Arrange
        var mockStore = new Mock<IX509Store>();
        var certCollection = new X509Certificate2Collection();
        mockStore.Setup(s => s.Certificates).Returns(certCollection);

        // Act & Assert
        Assert.Throws<Exception>(() => new LocalCert(mockStore.Object));
    }
}
