using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class AwsLambdaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AwsLambdaService> _logger;
    private const string LambdaFunctionUrl = "https://7vsdvlgm23v3ah2pa2kn3qmccq0ihhvd.lambda-url.us-east-1.on.aws/";

    public AwsLambdaService(HttpClient httpClient, ILogger<AwsLambdaService> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _logger = logger;
    }

    public async Task<bool> TriggerSnsNotificationAsync(string subject, string message)
    {
        try
        {
            var payload = new
            {
                subject,
                message
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(payload, options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(LambdaFunctionUrl, content);
            _logger.LogInformation($"✅ Lambda URL triggered. Status: {response.StatusCode}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calling Lambda Function URL");
            return false;
        }
    }
}
