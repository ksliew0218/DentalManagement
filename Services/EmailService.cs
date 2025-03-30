using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using DentalManagement.Areas.Patient.Models;
using DentalManagement.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace DentalManagement.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task SendConfirmationEmailAsync(string email, string firstName, string callbackUrl);
        Task SendAppointmentConfirmationEmailAsync(string email, string patientName, AppointmentDetailViewModel appointmentDetails);
        Task SendAppointmentCancellationEmailAsync(string email, string patientName, AppointmentDetailViewModel appointmentDetails);
        Task SendAppointmentReminderEmailAsync(string email, string patientName, AppointmentDetailViewModel appointmentDetails, string reminderType, string templateName);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly EmailTemplateService _templateService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger,
            HtmlEncoder htmlEncoder,
            EmailTemplateService templateService,
            IHttpContextAccessor httpContextAccessor)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _htmlEncoder = htmlEncoder;
            _templateService = templateService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                var mail = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };

                mail.To.Add(new MailAddress(email));

                using (var client = new SmtpClient(_emailSettings.Host, _emailSettings.Port))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
                    client.EnableSsl = _emailSettings.EnableSsl;

                    await client.SendMailAsync(mail);
                }

                _logger.LogInformation($"Email sent successfully to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {email}");
                // Don't throw here so that email failures don't break application flow
            }
        }

        public async Task SendConfirmationEmailAsync(string email, string firstName, string callbackUrl)
        {
            try
            {
                var encodedUrl = _htmlEncoder.Encode(callbackUrl);
                
                var replacements = new Dictionary<string, string>
                {
                    { "PatientName", string.IsNullOrEmpty(firstName) ? "there" : firstName },
                    { "CallbackUrl", encodedUrl },
                    { "CurrentYear", DateTime.Now.Year.ToString() }
                };
                
                string emailContent = await _templateService.GetEmailTemplateAsync("AccountConfirmation", replacements);
                
                if (!string.IsNullOrEmpty(emailContent))
                {
                    await SendEmailAsync(email, "Confirm Your SmileCraft Account", emailContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending confirmation email to {email}");
            }
        }

        public async Task SendAppointmentConfirmationEmailAsync(string email, string patientName, AppointmentDetailViewModel appointmentDetails)
        {
            try
            {
                // Get base URL for links
                var baseUrl = GetBaseUrl();
                var appointmentsUrl = $"{baseUrl}/Patient/Appointments";
                
                // Prepare template replacements
                var replacements = new Dictionary<string, string>
                {
                    { "PatientName", string.IsNullOrEmpty(patientName) ? "there" : patientName },
                    { "AppointmentId", appointmentDetails.Id.ToString() },
                    { "TreatmentName", appointmentDetails.TreatmentName },
                    { "AppointmentDate", appointmentDetails.FormattedAppointmentDate },
                    { "AppointmentTime", appointmentDetails.FormattedAppointmentTime },
                    { "DoctorName", appointmentDetails.DoctorName },
                    { "DoctorSpecialization", appointmentDetails.DoctorSpecialization ?? "" },
                    { "TreatmentDuration", appointmentDetails.TreatmentDuration.ToString() },
                    { "AppointmentsUrl", appointmentsUrl },
                    { "CurrentYear", DateTime.Now.Year.ToString() }
                };
                
                // Get the email content
                string emailContent = await _templateService.GetEmailTemplateAsync("AppointmentConfirmation", replacements);
                
                if (!string.IsNullOrEmpty(emailContent))
                {
                    // Send the email
                    await SendEmailAsync(email, "Your Appointment Confirmation - SmileCraft Dental", emailContent);
                }
                else
                {
                    _logger.LogWarning($"Failed to generate appointment confirmation email content for {email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending appointment confirmation email to {email}");
            }
        }

        public async Task SendAppointmentCancellationEmailAsync(string email, string patientName, AppointmentDetailViewModel appointmentDetails)
        {
            try
            {
                // Get base URL for links
                var baseUrl = GetBaseUrl();
                var bookAppointmentUrl = $"{baseUrl}/Patient/Appointments/Book";
                
                // Prepare template replacements
                var replacements = new Dictionary<string, string>
                {
                    { "PatientName", string.IsNullOrEmpty(patientName) ? "there" : patientName },
                    { "AppointmentId", appointmentDetails.Id.ToString() },
                    { "TreatmentName", appointmentDetails.TreatmentName },
                    { "AppointmentDate", appointmentDetails.FormattedAppointmentDate },
                    { "AppointmentTime", appointmentDetails.FormattedAppointmentTime },
                    { "DoctorName", appointmentDetails.DoctorName },
                    { "BookAppointmentUrl", bookAppointmentUrl },
                    { "CurrentYear", DateTime.Now.Year.ToString() }
                };
                
                // Get the email content
                string emailContent = await _templateService.GetEmailTemplateAsync("AppointmentCancellation", replacements);
                
                if (!string.IsNullOrEmpty(emailContent))
                {
                    // Send the email
                    await SendEmailAsync(email, "Your Appointment Cancellation - SmileCraft Dental", emailContent);
                }
                else
                {
                    _logger.LogWarning($"Failed to generate appointment cancellation email content for {email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending appointment cancellation email to {email}");
            }
        }

        public async Task SendAppointmentReminderEmailAsync(string email, string patientName, AppointmentDetailViewModel appointmentDetails, string reminderType, string templateName)
        {
            try
            {
                // Get base URL for links
                var baseUrl = GetBaseUrl();
                var appointmentDetailsUrl = $"{baseUrl}/Patient/Appointments/Details/{appointmentDetails.Id}";
                
                // Prepare template replacements
                var replacements = new Dictionary<string, string>
                {
                    { "PatientName", string.IsNullOrEmpty(patientName) ? "there" : patientName },
                    { "AppointmentId", appointmentDetails.Id.ToString() },
                    { "TreatmentName", appointmentDetails.TreatmentName },
                    { "AppointmentDate", appointmentDetails.FormattedAppointmentDate },
                    { "AppointmentTime", appointmentDetails.FormattedAppointmentTime },
                    { "DoctorName", appointmentDetails.DoctorName },
                    { "DoctorSpecialization", appointmentDetails.DoctorSpecialization ?? "" },
                    { "TreatmentDuration", appointmentDetails.TreatmentDuration.ToString() },
                    { "AppointmentDetailsUrl", appointmentDetailsUrl },
                    { "ReminderType", reminderType },
                    { "CurrentYear", DateTime.Now.Year.ToString() }
                };
                
                // Use templateName if provided, otherwise use a default
                string templateToUse = !string.IsNullOrEmpty(templateName) ? 
                    templateName : "AppointmentReminder";
                
                // Get the email content
                string emailContent = await _templateService.GetEmailTemplateAsync(templateToUse, replacements);
                
                if (!string.IsNullOrEmpty(emailContent))
                {
                    // Send the email
                    string subject = $"Appointment Reminder - Your appointment is {reminderType} away";
                    await SendEmailAsync(email, subject, emailContent);
                    
                    _logger.LogInformation($"Sent {reminderType} reminder email to {email} for appointment {appointmentDetails.Id}");
                }
                else
                {
                    // Log warning if template not found
                    _logger.LogWarning($"Failed to generate appointment reminder email content for {email} using template {templateName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending appointment reminder email to {email}");
            }
        }

        private string GetBaseUrl()
        {
            try
            {
                var request = _httpContextAccessor.HttpContext.Request;
                return $"{request.Scheme}://{request.Host}";
            }
            catch
            {
                return "https://www.smilecraftdental.com";  // Fallback if HttpContext is not available
            }
        }
    }
}