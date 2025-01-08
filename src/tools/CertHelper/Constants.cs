using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertHelper;
public class Constants
{
    public static readonly string Cert1Name = "LabCert1";
    public static readonly string Cert2Name = "LabCert2";
    public static readonly Uri Cert1Id = new Uri("https://test.vault.azure.net/certificates/LabCert1/07a7d98bf4884e5c40e690e02b96b3b4");
    public static readonly Uri Cert2Id = new Uri("https://test.vault.azure.net/certificates/LabCert2/07a7d98bf4884e5c40e690e02b96b3b4");
}
