using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ConverisIntegration.Services;

public interface ITykGatewayService
{
    Task<T?> GetDataAsync<T>(string endpoint);
}

public class TykGatewayService : ITykGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TykGatewayService> _logger;
    private readonly string _baseUrl;

    public TykGatewayService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TykGatewayService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["TykGatewayBaseUrl"] 
            ?? throw new InvalidOperationException("TykGatewayBaseUrl configuration is missing");
    }

    public async Task<T?> GetDataAsync<T>(string endpoint)
    {
        try
        {
            var url = $"{_baseUrl.TrimEnd('/')}";///{endpoint.TrimStart('/')}";
            _logger.LogInformation("Calling Tyk Gateway endpoint with mTLS: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            // Log response details before throwing
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Tyk Gateway returned {StatusCode} for endpoint: {Endpoint}. Response: {Response}", 
                    (int)response.StatusCode, 
                    endpoint, 
                    errorContent);
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Successfully received response from Tyk Gateway");

            var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result;
        }
        catch (HttpRequestException ex)
        {
            // mTLS failures typically manifest as HttpRequestExceptions
            _logger.LogError(ex, 
                "HTTP request to Tyk Gateway failed for endpoint: {Endpoint}. " +
                "If this is an SSL/TLS error, verify the client certificate is valid and trusted by the server. " +
                "Error: {Message}", 
                endpoint, 
                ex.Message);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize JSON response from endpoint: {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Tyk Gateway endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
}
