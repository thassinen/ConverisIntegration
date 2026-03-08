using ConverisIntegration.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register certificate service
builder.Services.AddSingleton<ICertificateService, KeyVaultCertificateService>();

// Register HttpClient with mTLS certificate for Tyk Gateway
builder.Services.AddHttpClient<ITykGatewayService, TykGatewayService>()
    .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
    {
        var certificateService = serviceProvider.GetRequiredService<ICertificateService>();
        var configuration = serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();

        var certificateName = configuration["TykCertificateName"] 
            ?? throw new InvalidOperationException("TykCertificateName configuration is missing");

        try
        {
            // Retrieve the certificate synchronously during startup
            logger.LogInformation("Configuring HttpClient with mTLS certificate: {CertificateName}", certificateName);
            var certificate = certificateService.GetCertificateAsync(certificateName).GetAwaiter().GetResult();

            logger.LogInformation("Certificate loaded successfully. Subject: {Subject}, Thumbprint: {Thumbprint}, HasPrivateKey: {HasPrivateKey}", 
                certificate.Subject, 
                certificate.Thumbprint, 
                certificate.HasPrivateKey);

            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(certificate);

            logger.LogInformation("HttpClientHandler configured with {Count} client certificate(s)", handler.ClientCertificates.Count);

            // Optional: Configure server certificate validation
            // handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true; // Use with caution

            return handler;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to configure HttpClient with certificate");
            throw;
        }
    });

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
