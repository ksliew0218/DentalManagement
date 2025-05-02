using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DentalManagement.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalManagement.Services
{
    public interface IAppointmentDocumentService
    {
        Task<bool> UploadDocumentAsync(int appointmentId, IFormFile file, string uploadedBy);
        Task<List<AppointmentDocument>> GetAppointmentDocumentsAsync(int appointmentId);
        Task<AppointmentDocument> GetDocumentByIdAsync(int id);
    }

    public class AppointmentDocumentService : IAppointmentDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IS3UploadService _s3UploadService;
        private readonly ILogger<AppointmentDocumentService> _logger;

        public AppointmentDocumentService(
            ApplicationDbContext context,
            IS3UploadService s3UploadService,
            ILogger<AppointmentDocumentService> logger)
        {
            _context = context;
            _s3UploadService = s3UploadService;
            _logger = logger;
        }

        public async Task<bool> UploadDocumentAsync(int appointmentId, IFormFile file, string uploadedBy)
        {
            try
            {
                // Verify the appointment exists
                var appointment = await _context.Appointments.FindAsync(appointmentId);
                if (appointment == null)
                {
                    _logger.LogError($"Appointment with ID {appointmentId} not found");
                    return false;
                }

                // Generate a unique S3 key - use a consistent format
                string fileName = Path.GetFileName(file.FileName);
                string sanitizedFileName = SanitizeFileName(fileName);
                string uniqueId = Guid.NewGuid().ToString("N"); // No dashes, just alphanumeric
                string s3Key = $"appointments/{appointmentId}/{uniqueId}/{sanitizedFileName}";

                _logger.LogInformation($"Generated S3 key for upload: {s3Key}");

                // Read the file into memory
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Convert to base64 for API transmission
                byte[] fileBytes = memoryStream.ToArray();
                string base64File = Convert.ToBase64String(fileBytes);

                // Create upload payload
                var uploadData = new
                {
                    FileName = sanitizedFileName,
                    ContentType = file.ContentType,
                    FileContent = base64File,
                    S3Key = s3Key
                };

                // Upload to S3 via Lambda function
                bool uploaded = await _s3UploadService.UploadToS3Async(uploadData);

                if (uploaded)
                {
                    // Create database record
                    var document = new AppointmentDocument
                    {
                        AppointmentId = appointmentId,
                        DocumentName = fileName, // Keep the original name for display
                        S3Key = s3Key, // Store the exact S3 key used for upload
                        ContentType = file.ContentType,
                        FileSize = file.Length,
                        UploadedDate = DateTime.UtcNow,
                        UploadedBy = uploadedBy
                    };

                    _context.AppointmentDocuments.Add(document);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Document {fileName} uploaded to S3 for appointment {appointmentId} with key {s3Key}");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to upload document {fileName} to S3");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading document for appointment {appointmentId}");
                return false;
            }
        }

        // Helper method to sanitize filenames for S3
        private string SanitizeFileName(string fileName)
        {
            // Replace spaces with underscores and remove any problematic characters
            return fileName
                .Replace(" ", "_")
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(":", "_")
                .Replace("*", "_")
                .Replace("?", "_")
                .Replace("\"", "_")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace("|", "_");
        }

        public async Task<List<AppointmentDocument>> GetAppointmentDocumentsAsync(int appointmentId)
        {
            return await _context.AppointmentDocuments
                .Where(d => d.AppointmentId == appointmentId)
                .OrderByDescending(d => d.UploadedDate)
                .ToListAsync();
        }

        public async Task<AppointmentDocument> GetDocumentByIdAsync(int id)
        {
            return await _context.AppointmentDocuments
                .FirstOrDefaultAsync(d => d.Id == id);
        }
    }
} 