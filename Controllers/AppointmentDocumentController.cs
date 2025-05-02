using System.Security.Claims;
using System.Threading.Tasks;
using DentalManagement.Models;
using DentalManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;

namespace DentalManagement.Controllers
{
    [Authorize]
    public class AppointmentDocumentController : Controller
    {
        private readonly IAppointmentDocumentService _documentService;
        private readonly IS3UploadService _s3UploadService;
        private readonly IS3DownloadService _s3DownloadService;
        private readonly ILogger<AppointmentDocumentController> _logger;
        private readonly HttpClient _httpClient;

        public AppointmentDocumentController(
            IAppointmentDocumentService documentService,
            IS3UploadService s3UploadService,
            IS3DownloadService s3DownloadService,
            ILogger<AppointmentDocumentController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _documentService = documentService;
            _s3UploadService = s3UploadService;
            _s3DownloadService = s3DownloadService;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet]
        public async Task<IActionResult> Index(int appointmentId)
        {
            var documents = await _documentService.GetAppointmentDocumentsAsync(appointmentId);
            ViewBag.AppointmentId = appointmentId;
            return View(documents);
        }

        [HttpGet]
        public IActionResult Upload(int appointmentId)
        {
            ViewBag.AppointmentId = appointmentId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(int appointmentId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Upload), new { appointmentId });
            }

            string uploadedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            bool result = await _documentService.UploadDocumentAsync(appointmentId, file, uploadedBy);

            if (result)
            {
                TempData["Success"] = "Document uploaded successfully to S3 via AWS Lambda!";
                return RedirectToAction(nameof(Index), new { appointmentId });
            }
            else
            {
                TempData["Error"] = "Failed to upload document. Please try again.";
                return RedirectToAction(nameof(Upload), new { appointmentId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var document = await _documentService.GetDocumentByIdAsync(id);
                
                if (document == null)
                {
                    _logger.LogWarning($"Document with ID {id} not found");
                    return NotFound("Document not found");
                }

                _logger.LogInformation($"Attempting to download document: {document.DocumentName}, S3Key: {document.S3Key}");
                
                var result = await _s3DownloadService.DownloadFromS3Async(document.S3Key);

                if (!result.Success)
                {
                    _logger.LogError($"Error downloading document: {result.Message}");
                    TempData["Error"] = $"Failed to download document from S3: {result.Message}";
                    return RedirectToAction(nameof(Index), new { appointmentId = document.AppointmentId });
                }

                if (string.IsNullOrEmpty(result.FileContent))
                {
                    _logger.LogError("Download succeeded but file content is empty");
                    TempData["Error"] = "The downloaded file is empty";
                    return RedirectToAction(nameof(Index), new { appointmentId = document.AppointmentId });
                }

                byte[] fileBytes;
                try 
                {
                    bool isBinaryContent = result.ContentType != null && 
                        (result.ContentType.StartsWith("image/") || 
                         result.ContentType.StartsWith("application/pdf") ||
                         result.ContentType.StartsWith("application/octet-stream") ||
                         result.ContentType.StartsWith("audio/") ||
                         result.ContentType.StartsWith("video/"));

                    if (isBinaryContent)
                    {
                        _logger.LogInformation($"Processing content as binary ({result.ContentType})");
                        fileBytes = System.Text.Encoding.UTF8.GetBytes(result.FileContent);
                    }
                    else if (IsBase64String(result.FileContent))
                    {
                        fileBytes = Convert.FromBase64String(result.FileContent);
                        _logger.LogInformation("Processing content as base64");
                    }
                    else
                    {
                        bool looksLikeJson = result.FileContent.TrimStart().StartsWith("{") || 
                                            result.FileContent.TrimStart().StartsWith("[");

                        if (looksLikeJson)
                        {
                            try
                            {
                                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(result.FileContent);
                                
                                if (jsonResponse.TryGetProperty("body", out var body))
                                {
                                    string bodyContent = body.GetString();
                                    _logger.LogInformation("Extracting body from JSON response");
                                    
                                    if (IsBase64String(bodyContent))
                                    {
                                        fileBytes = Convert.FromBase64String(bodyContent);
                                    }
                                    else
                                    {
                                        fileBytes = System.Text.Encoding.UTF8.GetBytes(bodyContent);
                                    }
                                }
                                else if (jsonResponse.TryGetProperty("fileContent", out var fileContent))
                                {
                                    string content = fileContent.GetString();
                                    _logger.LogInformation("Extracting fileContent from JSON response");
                                    
                                    if (IsBase64String(content))
                                    {
                                        fileBytes = Convert.FromBase64String(content);
                                    }
                                    else
                                    {
                                        fileBytes = System.Text.Encoding.UTF8.GetBytes(content);
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation("No recognized property in JSON, returning raw JSON");
                                    fileBytes = System.Text.Encoding.UTF8.GetBytes(result.FileContent);
                                }
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogInformation($"Not valid JSON, using as plain text: {ex.Message}");
                                fileBytes = System.Text.Encoding.UTF8.GetBytes(result.FileContent);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Content is not JSON or base64, using as plain text");
                            fileBytes = System.Text.Encoding.UTF8.GetBytes(result.FileContent);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file content");
                    fileBytes = System.Text.Encoding.UTF8.GetBytes(
                        $"Error processing file content: {ex.Message}\n\nRaw content: {result.FileContent}");
                }
                
                var finalContentType = result.ContentType ?? document.ContentType ?? "application/octet-stream";
                return File(fileBytes, finalContentType, document.DocumentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading document with ID {id}");
                TempData["Error"] = $"An error occurred while downloading the document: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        
        private bool IsBase64String(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;
                
            s = s.Trim();
            return (s.Length % 4 == 0) && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);
        }
    }
} 