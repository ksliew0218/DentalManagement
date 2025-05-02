using System.Threading.Tasks;
using DentalManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DentalManagement.Controllers
{
    [Authorize]
    public class S3UploadController : Controller
    {
        private readonly IS3UploadService _s3UploadService;
        private readonly ILogger<S3UploadController> _logger;

        public S3UploadController(IS3UploadService s3UploadService, ILogger<S3UploadController> logger)
        {
            _s3UploadService = s3UploadService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadData([FromBody] object data)
        {
            _logger.LogInformation("Attempting to upload data to S3 via Lambda function");
            
            bool success = await _s3UploadService.UploadToS3Async(data);
            
            if (success)
            {
                _logger.LogInformation("Data successfully uploaded to S3");
                return Ok(new { success = true, message = "Data successfully uploaded to S3" });
            }
            else
            {
                _logger.LogError("Failed to upload data to S3");
                return BadRequest(new { success = false, message = "Failed to upload data to S3" });
            }
        }
    }
} 