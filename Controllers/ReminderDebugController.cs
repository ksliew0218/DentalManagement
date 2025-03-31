using System;
using System.Threading.Tasks;
using DentalManagement.Models;
using DentalManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using DentalManagement.Areas.Patient.Models;
using Microsoft.AspNetCore.Authorization;
namespace DentalManagement.Controllers
{
    [AllowAnonymous]
    // This controller is only for testing - you can remove it or secure it in production
    [Route("api/debug/reminders")]
    [ApiController]
    public class ReminderDebugController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReminderDebugController> _logger;

        public ReminderDebugController(
            ApplicationDbContext context,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<ReminderDebugController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        // Test sending reminder for a specific appointment
        [HttpGet("test/{appointmentId}")]
        public async Task<IActionResult> TestReminder(int appointmentId)
        {
            _logger.LogInformation($"Testing reminder for appointment ID: {appointmentId}");
            
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                .Include(a => a.TreatmentType)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
                
            if (appointment == null)
            {
                _logger.LogWarning($"Appointment ID {appointmentId} not found");
                return NotFound($"Appointment ID {appointmentId} not found");
            }
            
            var result = new Dictionary<string, object>();
            result["appointment"] = new {
                Id = appointment.Id,
                PatientName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}",
                PatientEmail = appointment.Patient.User.Email,
                DoctorName = $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}",
                TreatmentName = appointment.TreatmentType.Name,
                AppointmentDate = appointment.AppointmentDate.ToString("yyyy-MM-dd"),
                AppointmentTime = appointment.AppointmentTime.ToString()
            };
            
            try
            {
                // Create reminder record
                var reminderRecord = new AppointmentReminder
                {
                    AppointmentId = appointment.Id,
                    ReminderType = "48hour",
                    SentAt = DateTime.UtcNow,
                    SentByEmail = false,
                    SentByPushNotification = false
                };
                
                _context.AppointmentReminders.Add(reminderRecord);
                await _context.SaveChangesAsync();
                result["reminderCreated"] = true;
                
                // Get user ID and information
                var userId = appointment.Patient.UserID;
                string doctorName = $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}";
                string treatmentName = appointment.TreatmentType.Name;
                
                // Create in-app notification
                var notification = await _notificationService.CreateAppointmentReminderNotificationAsync(
                    userId,
                    appointment.Id,
                    "48hour",
                    appointment.AppointmentDate,
                    appointment.AppointmentTime,
                    treatmentName,
                    doctorName);
                    
                reminderRecord.SentByPushNotification = true;
                result["notificationCreated"] = true;
                
                // Get the user's notification preferences
                var preferences = await _context.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                
                if (preferences == null)
                {
                    preferences = new UserNotificationPreferences
                    {
                        UserId = userId,
                        EmailAppointmentReminders = true,
                        EmailNewAppointments = true,
                        EmailAppointmentChanges = true,
                        EmailPromotions = true,
                        LastUpdated = DateTime.UtcNow
                    };
                    
                    _context.UserNotificationPreferences.Add(preferences);
                    await _context.SaveChangesAsync();
                    result["preferencesCreated"] = true;
                }
                
                result["emailEnabled"] = preferences.EmailAppointmentReminders;
                
                // Send email if enabled
                if (preferences.EmailAppointmentReminders)
                {
                    // Create appointment details for email
                    var appointmentDetails = new AppointmentDetailViewModel
                    {
                        Id = appointment.Id,
                        TreatmentName = treatmentName,
                        DoctorName = doctorName,
                        DoctorSpecialization = appointment.Doctor.Specialty,
                        AppointmentDate = appointment.AppointmentDate,
                        AppointmentTime = appointment.AppointmentTime,
                        Status = appointment.Status,
                        CreatedOn = appointment.CreatedAt,
                        TreatmentCost = appointment.TreatmentType.Price,
                        TreatmentDuration = appointment.TreatmentType.Duration
                    };
                    
                    // Get patient information
                    var patientName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}";
                    var patientEmail = appointment.Patient.User.Email;
                    
                    // Send reminder email
                    await _emailService.SendAppointmentReminderEmailAsync(
                        patientEmail, 
                        patientName, 
                        appointmentDetails, 
                        "48-hour",
                        "Appointment48HourReminder");
                    
                    reminderRecord.SentByEmail = true;
                    
                    // Update the notification to track email sent
                    if (notification != null)
                    {
                        notification.EmailSent = true;
                        notification.EmailSentAt = DateTime.UtcNow;
                    }
                    
                    result["emailSent"] = true;
                }
                
                // Save changes
                await _context.SaveChangesAsync();
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending reminder for appointment ID {appointmentId}");
                result["error"] = ex.Message;
                result["stackTrace"] = ex.StackTrace;
                return StatusCode(500, result);
            }
        }
        
        // Test finding appointments that need reminders
        [HttpGet("find-appointments")]
        public async Task<IActionResult> FindAppointmentsForReminders()
        {
            try
            {
                var targetDate = DateTime.UtcNow.AddDays(2).Date;
                
                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Doctor)
                    .Include(a => a.TreatmentType)
                    .Include(a => a.Reminders)
                    .Where(a => 
                        a.AppointmentDate.Date == targetDate &&
                        a.Status != "Cancelled" &&
                        !a.Reminders.Any(r => r.ReminderType == "48hour"))
                    .Select(a => new {
                        Id = a.Id,
                        PatientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                        PatientEmail = a.Patient.User.Email,
                        DoctorName = $"Dr. {a.Doctor.FirstName} {a.Doctor.LastName}",
                        TreatmentName = a.TreatmentType.Name,
                        AppointmentDate = a.AppointmentDate.ToString("yyyy-MM-dd"),
                        AppointmentTime = a.AppointmentTime.ToString()
                    })
                    .ToListAsync();
                
                return Ok(new {
                    TargetDate = targetDate.ToString("yyyy-MM-dd"),
                    AppointmentsFound = appointments.Count,
                    Appointments = appointments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding appointments for reminders");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
        
        // Test template rendering (without sending an email)
        [HttpGet("test-template")]
        public async Task<IActionResult> TestTemplate()
        {
            try
            {
                // Sample data for testing template rendering
                var replacements = new Dictionary<string, string>
                {
                    { "PatientName", "John Doe" },
                    { "AppointmentId", "12345" },
                    { "TreatmentName", "Dental Cleaning" },
                    { "AppointmentDate", "Wednesday, April 2, 2025" },
                    { "AppointmentTime", "1:00 PM" },
                    { "DoctorName", "Dr. Jane Smith" },
                    { "DoctorSpecialization", "General Dentistry" },
                    { "TreatmentDuration", "60" },
                    { "AppointmentDetailsUrl", "https://example.com/appointments/12345" },
                    { "ReminderType", "48-hour" },
                    { "CurrentYear", DateTime.Now.Year.ToString() }
                };
                
                // Get the email template service
                var templateService = HttpContext.RequestServices.GetService(typeof(EmailTemplateService)) as EmailTemplateService;
                if (templateService == null)
                {
                    return BadRequest("EmailTemplateService not found");
                }
                
                // Get email content
                string emailContent = await templateService.GetEmailTemplateAsync("Appointment48HourReminder", replacements);
                
                if (string.IsNullOrEmpty(emailContent))
                {
                    return NotFound("Email template not found or could not be rendered");
                }
                
                // Return the rendered HTML
                return Content(emailContent, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing email template");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}