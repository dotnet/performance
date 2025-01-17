using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CertHelper;
public interface IX509Store
{
    X509Certificate2Collection Certificates { get; }
    string? Name { get; }
    StoreLocation Location { get; }
    X509Store GetX509Store();
}

public class TestableX509Store : IX509Store
{
    public X509Certificate2Collection Certificates { get => store.Certificates; }

    public string? Name => store.Name;

    public StoreLocation Location => store.Location;

    private X509Store store;
    public TestableX509Store(OpenFlags flags = OpenFlags.ReadOnly)
    {
        store = new X509Store(StoreName.My, StoreLocation.CurrentUser, flags);
    }

    public X509Store GetX509Store()
    {
        return store;
    }
}
