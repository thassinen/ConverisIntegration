using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace ConverisIntegration.Services;

public class KeyVaultCertificateService : ICertificateService
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<KeyVaultCertificateService> _logger;

    public KeyVaultCertificateService(
        IConfiguration configuration,
        ILogger<KeyVaultCertificateService> logger)
    {
        var keyVaultUrl = configuration["KeyVaultUrl"] 
            ?? throw new InvalidOperationException("KeyVaultUrl configuration is missing");

        // Configure DefaultAzureCredential to exclude problematic credentials for local development
        var credentialOptions = new DefaultAzureCredentialOptions
        {
            ExcludeVisualStudioCredential = false,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeAzurePowerShellCredential = true,
            ExcludeAzureDeveloperCliCredential = true,
            ExcludeManagedIdentityCredential = false, // Keep for Azure deployment
            ExcludeEnvironmentCredential = false, // Enable for service principal auth
            ExcludeWorkloadIdentityCredential = true
        };

        _secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential(credentialOptions));
        _logger = logger;
    }

    public async Task<X509Certificate2> GetCertificateAsync(string certificateName)
    {
        try
        {
            _logger.LogInformation("Retrieving certificate '{CertificateName}' from Key Vault", certificateName);
            
            // Key Vault stores certificates as secrets in PFX format
            KeyVaultSecret secret = await _secretClient.GetSecretAsync(certificateName);
            
            // Convert the base64-encoded PFX to X509Certificate2
            byte[] pfxBytes = Convert.FromBase64String(secret.Value);
            var certificate = new X509Certificate2(pfxBytes, (string?)null, 
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
            
            _logger.LogInformation("Successfully retrieved certificate '{CertificateName}'", certificateName);
            
            return certificate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve certificate '{CertificateName}' from Key Vault", certificateName);
            throw;
        }
    }
}
