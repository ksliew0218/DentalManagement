using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace DentalManagement.Services
{
    public interface IS3DownloadService
    {
        Task<S3DownloadResult> DownloadFromS3Async(string s3Key);
    }

    public class S3DownloadService : IS3DownloadService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<S3DownloadService> _logger;

        public S3DownloadService(HttpClient httpClient, ILogger<S3DownloadService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<S3DownloadResult> DownloadFromS3Async(string s3Key)
        {
            try
            {
                _logger.LogInformation($"Attempting to download with S3 key: {s3Key}");
                
                var result = await AttemptDownloadWithKey(s3Key);
                
                if (!result.Success && result.Message.Contains("NoSuchKey"))
                {
                    _logger.LogInformation("First attempt failed with NoSuchKey, trying alternative encodings");
                    
                    string doubleEncodedKey = Uri.EscapeDataString(Uri.EscapeDataString(s3Key));
                    _logger.LogInformation($"Trying with double encoded key: {doubleEncodedKey}");
                    result = await AttemptDownloadWithKey(doubleEncodedKey);
                    
                    if (!result.Success && result.Message.Contains("NoSuchKey"))
                    {
                        string fileName = s3Key.Split('/').Last();
                        _logger.LogInformation($"Trying with just filename: {fileName}");
                        result = await AttemptDownloadWithKey(fileName);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading from S3: {ex.Message}");
                return new S3DownloadResult
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        
        private async Task<S3DownloadResult> AttemptDownloadWithKey(string key)
        {
            try
            {
                var requestUrl = $"https://pig7opioh5.execute-api.us-east-1.amazonaws.com/prod_/DownloadFromS3?key={Uri.EscapeDataString(key)}";
                _logger.LogInformation($"Request URL: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Response status: {response.StatusCode}");
                
                if (responseContent.Length > 200)
                {
                    _logger.LogInformation($"Response content (truncated): {responseContent.Substring(0, 200)}...");
                }
                else
                {
                    _logger.LogInformation($"Response content: {responseContent}");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return new S3DownloadResult
                    {
                        Success = false,
                        Message = $"Failed to download file: {response.StatusCode}, Content: {responseContent}"
                    };
                }

                var contentType = response.Content.Headers.ContentType?.MediaType?.ToLower();
                _logger.LogInformation($"Response content type: {contentType}");

                bool isBinaryContent = contentType != null && 
                    (contentType.StartsWith("image/") || 
                     contentType.StartsWith("application/pdf") ||
                     contentType.StartsWith("application/octet-stream") ||
                     contentType.StartsWith("audio/") ||
                     contentType.StartsWith("video/"));

                if (isBinaryContent)
                {
                    return new S3DownloadResult
                    {
                        Success = true,
                        Message = "Binary download successful",
                        FileContent = responseContent,
                        ContentType = contentType ?? "application/octet-stream"
                    };
                }

                // Check if it looks like JSON (starts with '{' or '[')
                bool looksLikeJson = responseContent.TrimStart().StartsWith("{") || 
                                     responseContent.TrimStart().StartsWith("[");

                if (looksLikeJson)
                {
                    try
                    {
                        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (jsonResponse.TryGetProperty("body", out var body) && 
                            jsonResponse.TryGetProperty("statusCode", out var statusCode))
                        {
                            string bodyContent = body.GetString();
                            int status = statusCode.GetInt32();
                            
                            if (status >= 200 && status < 300)
                            {
                                return new S3DownloadResult
                                {
                                    Success = true,
                                    Message = "Download successful",
                                    FileContent = bodyContent,
                                    ContentType = "application/octet-stream" 
                                };
                            }
                            else
                            {
                                return new S3DownloadResult
                                {
                                    Success = false,
                                    Message = $"Lambda returned error: {bodyContent}"
                                };
                            }
                        }
                        
                        return new S3DownloadResult
                        {
                            Success = true,
                            Message = "Download successful",
                            FileContent = responseContent,
                            ContentType = contentType ?? "application/json"
                        };
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning($"Failed to parse response as JSON: {ex.Message}");
                        // Fall through to treat as non-JSON content
                    }
                }

                // If not JSON or couldn't parse as JSON, return as is
                return new S3DownloadResult
                {
                    Success = true,
                    Message = "Download successful",
                    FileContent = responseContent,
                    ContentType = contentType ?? "application/octet-stream"
                };
            }
            catch (Exception ex)
            {
                return new S3DownloadResult
                {
                    Success = false,
                    Message = $"Error attempting download: {ex.Message}"
                };
            }
        }
    }

    public class S3DownloadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FileContent { get; set; }
        public string ContentType { get; set; }
    }
}