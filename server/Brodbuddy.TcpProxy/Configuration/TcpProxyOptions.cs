using System.Security.Cryptography.X509Certificates;
using Brodbuddy.TcpProxy.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brodbuddy.TcpProxy.Configuration;

public class TcpProxyOptions
{
    public IEndpoint? PublicEndpoint { get; set; }
    public bool EnableSsl { get; set; }
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
    public ILogger Logger { get; set; } = NullLogger.Instance;

    public X509Certificate2 GetCertificate()
    {
        if (!EnableSsl || string.IsNullOrEmpty(CertificatePath)) return null!;
        
        return string.IsNullOrEmpty(CertificatePassword) ? new X509Certificate2(CertificatePath) : new X509Certificate2(CertificatePath, CertificatePassword);
    }
}