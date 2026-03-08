using ConverisIntegration.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ConverisIntegration;

public class Function1
{
    private readonly ILogger<Function1> _logger;
    private readonly ITykGatewayService _tykGatewayService;

    public Function1(
        ILogger<Function1> logger,
        ITykGatewayService tykGatewayService)
    {
        _logger = logger;
        _tykGatewayService = tykGatewayService;
    }

    [Function("Function1")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        try
        {
            // Get endpoint from query parameter or use a default
            string endpoint = req.Query["endpoint"].FirstOrDefault() ?? "reports/getOrgList";

            // Call Tyk Gateway with mTLS authentication
            // Replace 'object' with your expected response type
            var data = await _tykGatewayService.GetDataAsync<object>(endpoint);

            return new OkObjectResult(data);
            /*
            return new OkObjectResult(new
            {
                Message = "Successfully retrieved data from Tyk Gateway",
                Data = data
            });
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request");
            return new ObjectResult(new
            {
                Error = "Failed to retrieve data from Tyk Gateway",
                Message = ex.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
