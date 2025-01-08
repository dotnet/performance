using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CertHelper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using(var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite))
            {
                foreach(var cert in store.Certificates)
                {
                    if (cert.Subject.Contains("CN=dotnetperf.microsoft.com"))
                    {
                        Console.WriteLine(Convert.ToBase64String(cert.Export(X509ContentType.Pfx)));
                    }
                }
            }
        }
    }
}
