using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace CertHelper;

/// <summary>
/// Abstraction over where the local perf certificates live. On Windows and Linux this is
/// backed by the CurrentUser\My <see cref="X509Store"/>. On macOS the CurrentUser\My store maps
/// to the login Keychain, which cannot export a private key back out without an interactive
/// password prompt, so a file-backed store is used instead.
/// </summary>
public interface ILocalCertStore
{
    /// <summary>
    /// Returns the certificates currently persisted in the local store.
    /// </summary>
    X509Certificate2Collection GetCertificates();

    /// <summary>
    /// Replaces <paramref name="certsToRemove"/> with <paramref name="certsToAdd"/> in the local store.
    /// </summary>
    void Rotate(X509Certificate2Collection certsToRemove, X509Certificate2Collection certsToAdd);
}

/// <summary>
/// Picks the appropriate <see cref="ILocalCertStore"/> for the current operating system.
/// </summary>
public static class LocalCertStoreFactory
{
    public static ILocalCertStore Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new FileLocalCertStore();
        }

        return new X509LocalCertStore();
    }
}

/// <summary>
/// <see cref="ILocalCertStore"/> backed by the CurrentUser\My <see cref="X509Store"/>.
/// Used on Windows and Linux where the managed store fully supports adding certificates with
/// private keys and exporting them back out.
/// </summary>
public class X509LocalCertStore : ILocalCertStore
{
    private readonly IX509Store _store;

    public X509LocalCertStore(IX509Store? store = null)
    {
        _store = store ?? new TestableX509Store();
    }

    public X509Certificate2Collection GetCertificates()
    {
        return _store.Certificates;
    }

    public void Rotate(X509Certificate2Collection certsToRemove, X509Certificate2Collection certsToAdd)
    {
        var store = _store.GetX509Store();
        store.Open(OpenFlags.ReadWrite);
        try
        {
            store.RemoveRange(certsToRemove);
            store.AddRange(certsToAdd);
        }
        finally
        {
            store.Close();
        }
    }
}

/// <summary>
/// <see cref="ILocalCertStore"/> that persists certificates as PFX files in a known directory.
/// Used on macOS where the OS certificate store (login Keychain) cannot export a private key
/// without an interactive password prompt. Files are named by thumbprint so that the on-disk
/// state can be reconciled exactly with the desired set on rotation.
/// </summary>
public class FileLocalCertStore : ILocalCertStore
{
    // Matches the directory the perf Mac hosts historically used for pre-provisioned certificates.
    public const string DefaultDirectory = "/Users/helix-runner/certs";

    private readonly string _directory;

    public FileLocalCertStore(string? directory = null)
    {
        _directory = directory ?? DefaultDirectory;
    }

    public X509Certificate2Collection GetCertificates()
    {
        var certificates = new X509Certificate2Collection();
        if (!Directory.Exists(_directory))
        {
            return certificates;
        }

        foreach (var file in Directory.EnumerateFiles(_directory, "*.pfx"))
        {
            try
            {
                var bytes = File.ReadAllBytes(file);
#if NET9_0_OR_GREATER
                certificates.Add(X509CertificateLoader.LoadPkcs12(bytes, "", X509KeyStorageFlags.Exportable));
#else
                certificates.Add(new X509Certificate2(bytes, "", X509KeyStorageFlags.Exportable));
#endif
            }
            catch (Exception ex)
            {
                // Skip files that cannot be loaded; they will be treated as missing and trigger bootstrap/rotation.
                Console.Error.WriteLine($"Failed to load certificate from {file}: {ex.Message}");
            }
        }

        return certificates;
    }

    public void Rotate(X509Certificate2Collection certsToRemove, X509Certificate2Collection certsToAdd)
    {
        Directory.CreateDirectory(_directory);

        // Clear the existing PFX files so the on-disk state matches the desired set exactly.
        foreach (var file in Directory.EnumerateFiles(_directory, "*.pfx"))
        {
            File.Delete(file);
        }

        foreach (var cert in certsToAdd)
        {
            var path = Path.Combine(_directory, $"{cert.Thumbprint}.pfx");
            File.WriteAllBytes(path, cert.Export(X509ContentType.Pfx));
        }
    }
}
