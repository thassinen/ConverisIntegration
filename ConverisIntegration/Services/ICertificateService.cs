using System.Security.Cryptography.X509Certificates;

namespace ConverisIntegration.Services;

public interface ICertificateService
{
    Task<X509Certificate2> GetCertificateAsync(string certificateName);
}
