using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CertHelper;

public class KeyVaultCert
{
    private readonly string KeyVaultUrl = "https://dotnetperfkeyvault.vault.azure.net/";
    private readonly string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
    private readonly string ClientId = "8c4b65ef-5a73-4d5a-a298-962d4a4ef7bc";

    public X509Certificate2Collection KeyVaultCertificates { get; set; }
    public ILocalCert LocalCerts { get; set; }
    private TokenCredential Credential { get; set; }
    private CertificateClient CertClient { get; set; }
    private SecretClient SecretClient { get; set; }

    public KeyVaultCert(TokenCredential? cred = null, CertificateClient? certClient = null, SecretClient? secretClient = null, ILocalCert? localCerts = null)
    {
        LocalCerts = localCerts ?? new LocalCert();
        Credential = cred ?? GetCertifcateCredentialAsync(TenantId, ClientId, LocalCerts.Certificates).Result;
        CertClient = certClient ?? new CertificateClient(new Uri(KeyVaultUrl), Credential);
        SecretClient = secretClient ?? new SecretClient(new Uri(KeyVaultUrl), Credential);
        KeyVaultCertificates = new X509Certificate2Collection();
    }

    public async Task GetKeyVaultCerts()
    {
        KeyVaultCertificates.Add(await FindCertificateInKeyVault(Constants.Cert1Name));
        KeyVaultCertificates.Add(await FindCertificateInKeyVault(Constants.Cert2Name));

        if (KeyVaultCertificates.Count != 2 || KeyVaultCertificates.Where(c => c == null).Count() > 0)
        {
            throw new Exception("One or more certificates not found");
        }
    }

    private async Task<ClientCertificateCredential> GetCertifcateCredentialAsync(string tenantId, string clientId, X509Certificate2Collection certCollection)
    {
        ClientCertificateCredential? ccc = null;
        Exception? exception = null;
        foreach (var cert in certCollection)
        {
            try
            {
                ccc = new ClientCertificateCredential(tenantId, clientId, cert);
                await ccc.GetTokenAsync(new TokenRequestContext(new string[] { "https://vault.azure.net/.default" }));
                break;
            }
            catch (Exception ex)
            {
                ccc = null;
                exception = ex;
            }
        }
        if(ccc == null)
        {
            throw new Exception("Both certificates failed to authenticate", exception);
        }
        return ccc;
    }

    private async Task<X509Certificate2> FindCertificateInKeyVault(string certName)
    {
        var keyVaultCert = await CertClient.GetCertificateAsync(certName);
        if(keyVaultCert.Value == null)
        {
            throw new Exception("Certificate not found in Key Vault");
        }
        var secret = await SecretClient.GetSecretAsync(keyVaultCert.Value.Name, keyVaultCert.Value.SecretId.Segments.Last());
        if(secret.Value == null)
        {
            throw new Exception("Certificate secret not found in Key Vault");
        }
        var certBytes = Convert.FromBase64String(secret.Value.Value);
#if NET9_0_OR_GREATER        
        var cert = X509CertificateLoader.LoadPkcs12(certBytes, "", X509KeyStorageFlags.Exportable);
#else
        var cert = new X509Certificate2(certBytes, "", X509KeyStorageFlags.Exportable);
#endif
        return cert;
    }

    public bool ShouldRotateCerts()
    {
        var keyVaultThumbprints = new HashSet<string>();
        foreach (var cert in KeyVaultCertificates)
        {
            keyVaultThumbprints.Add(cert.Thumbprint);
        }
        foreach(var cert in LocalCerts.Certificates)
        {
            if (!keyVaultThumbprints.Contains(cert.Thumbprint))
            {
                return true;
            }
        }
        return false;
    }
}
