using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using DentalManagement.Areas.Patient.Models;
using DentalManagement.Areas.Doctor.Models;
using DentalManagement.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using DentalManagement.Services;

namespace DentalManagement.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task SendConfirmationEmailAsync(string email, string firstName, string callbackUrl);
        Task SendAppointmentConfirmationEmailAsync(string email, string patientName, AppointmentDetailViewModel appointmentDetails);
        Task SendAppointmentCancellationEmailAsync(string email, string patientName, AppointmentDetailViewModel appointmentDetails);
        Task SendAppointmentReminderEmailAsync(string email, string patientName, AppointmentDetailViewModel appointmentDetails, string reminderType, string templateName);
        Task SendAppointmentCompletedEmailAsync(string email, string patientName, AppointmentDetailViewModel appointmentDetails, decimal remainingBalance = 0);
        Task SendDoctorAppointmentNotificationAsync(DoctorAppointmentNotificationViewModel appointmentNotification);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly EmailTemplateService _templateService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AwsLambdaService _awsLambdaService;


        public EmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger,
            HtmlEncoder htmlEncoder,
            EmailTemplateService templateService,
            IHttpContextAccessor httpContextAccessor,
            AwsLambdaService awsLambdaService)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _htmlEncoder = htmlEncoder;
            _templateService = templateService;
            _httpContextAccessor = httpContextAccessor;
            _awsLambdaService = awsLambdaService;
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
                var baseUrl = GetBaseUrl();
                var appointmentsUrl = $"{baseUrl}/Patient/Appointments";

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

                string emailContent = await _templateService.GetEmailTemplateAsync("AppointmentConfirmation", replacements);

                if (!string.IsNullOrEmpty(emailContent))
                {
                    await SendEmailAsync(email, "Your Appointment Confirmation - SmileCraft Dental", emailContent);
                    _logger.LogInformation($"Sent appointment confirmation email to {email} for appointment {appointmentDetails.Id}");

                    // 🔔 Call Lambda SNS after email success
                    string snsMessage = $"{patientName} has booked a {appointmentDetails.TreatmentName} appointment on {appointmentDetails.FormattedAppointmentDate} at {appointmentDetails.FormattedAppointmentTime}.";
                    string snsSubject = "New Appointment Booked";
                    await _awsLambdaService.TriggerSnsNotificationAsync(snsSubject, snsMessage);
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
                var baseUrl = GetBaseUrl();
                var bookAppointmentUrl = $"{baseUrl}/Patient/Appointments/Book";

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

                string emailContent = await _templateService.GetEmailTemplateAsync("AppointmentCancellation", replacements);

                if (!string.IsNullOrEmpty(emailContent))
                {
                    await SendEmailAsync(email, "Your Appointment Cancellation - SmileCraft Dental", emailContent);
                    _logger.LogInformation($"Sent appointment cancellation email to {email} for appointment {appointmentDetails.Id}");

                    // 🔔 Call Lambda SNS after cancellation
                    string snsMessage = $"{patientName} has cancelled their {appointmentDetails.TreatmentName} appointment on {appointmentDetails.FormattedAppointmentDate} at {appointmentDetails.FormattedAppointmentTime}.";
                    string snsSubject = "Appointment Cancelled";
                    await _awsLambdaService.TriggerSnsNotificationAsync(snsSubject, snsMessage);
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


        public async Task SendAppointmentCompletedEmailAsync(string email, string patientName, AppointmentDetailViewModel appointmentDetails, decimal remainingBalance = 0)
        {
            try
            {
                var baseUrl = GetBaseUrl();
                var bookAppointmentUrl = $"{baseUrl}/Patient/Appointments/Book";
                var paymentUrl = $"{baseUrl}/Patient/Appointments/Details/{appointmentDetails.Id}";
                
                string careInstructions = GetCareInstructionsForTreatment(appointmentDetails.TreatmentName);
                
                var replacements = new Dictionary<string, string>
                {
                    { "PatientName", string.IsNullOrEmpty(patientName) ? "there" : patientName },
                    { "AppointmentId", appointmentDetails.Id.ToString() },
                    { "TreatmentName", appointmentDetails.TreatmentName },
                    { "AppointmentDate", appointmentDetails.FormattedAppointmentDate },
                    { "AppointmentTime", appointmentDetails.FormattedAppointmentTime },
                    { "DoctorName", appointmentDetails.DoctorName },
                    { "BookAppointmentUrl", bookAppointmentUrl },
                    { "PaymentUrl", paymentUrl },
                    { "RemainingBalance", remainingBalance.ToString("0.00") },
                    { "CareInstructions", careInstructions },
                    { "CurrentYear", DateTime.Now.Year.ToString() }
                };
                
                string emailContent = await _templateService.GetEmailTemplateAsync("AppointmentCompleted", replacements);
                
                if (!string.IsNullOrEmpty(emailContent))
                {
                    await SendEmailAsync(email, "Your Appointment Completed - SmileCraft Dental", emailContent);
                    _logger.LogInformation($"Sent completion email to {email} for appointment {appointmentDetails.Id}");
                }
                else
                {
                    _logger.LogWarning($"Failed to generate appointment completion email content for {email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending appointment completion email to {email}");
            }
        }

        public async Task SendAppointmentReminderEmailAsync(string email, string patientName, AppointmentDetailViewModel appointmentDetails, string reminderType, string templateName)
        {
            try
            {
                var baseUrl = GetBaseUrl();
                var appointmentDetailsUrl = $"{baseUrl}/Patient/Appointments/Details/{appointmentDetails.Id}";
                
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
                
                string templateToUse = !string.IsNullOrEmpty(templateName) ? 
                    templateName : "AppointmentReminder";
                
                string emailContent = await _templateService.GetEmailTemplateAsync(templateToUse, replacements);
                
                if (!string.IsNullOrEmpty(emailContent))
                {
                    string subject = $"Appointment Reminder - Your appointment is {reminderType} away";
                    await SendEmailAsync(email, subject, emailContent);
                    
                    _logger.LogInformation($"Sent {reminderType} reminder email to {email} for appointment {appointmentDetails.Id}");
                }
                else
                {
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
                return "https://www.smilecraftdental.com";  
            }
        }
        
        private string GetCareInstructionsForTreatment(string treatmentName)
        {
            switch (treatmentName.ToLower())
            {
                case "cleaning":
                case "dental cleaning":
                case "teeth cleaning":
                    return "• Maintain regular brushing (twice daily) and flossing (once daily)<br>" +
                           "• Avoid staining foods and beverages for 48 hours<br>" +
                           "• If you experience any sensitivity, use a desensitizing toothpaste";
                
                case "filling":
                case "dental filling":
                    return "• Avoid chewing on the treated side for at least 2 hours<br>" +
                           "• If you received a composite filling, avoid consuming colored foods/drinks for 48 hours<br>" +
                           "• Some sensitivity is normal for a few days - contact us if it persists longer";
                
                case "root canal":
                    return "• Take any prescribed medications as directed<br>" +
                           "• Avoid chewing on the treated tooth until your permanent restoration is placed<br>" +
                           "• Schedule your follow-up appointment for the permanent crown<br>" +
                           "• Contact us immediately if you experience severe pain or swelling";
                
                case "crown":
                case "dental crown":
                    return "• Avoid sticky or hard foods for 24 hours<br>" +
                           "• Brush and floss normally, but carefully around the crown area<br>" +
                           "• Some sensitivity is normal for a few days<br>" +
                           "• Contact us if your bite feels uneven or if the crown feels loose";
                
                default:
                    return "• Follow proper oral hygiene: brush twice daily and floss once daily<br>" +
                           "• Avoid hard, sticky, or very hot/cold foods for the next 24 hours<br>" +
                           "• Some sensitivity may be normal - use over-the-counter pain relief if needed<br>" +
                           "• Contact us if you experience severe pain, swelling, or have any concerns";
            }
        }

    public async Task SendDoctorAppointmentNotificationAsync(DoctorAppointmentNotificationViewModel appointmentNotification)
        {
            try
            {
                var baseUrl = GetBaseUrl();
                var appointmentDetailsUrl = $"{baseUrl}/Doctor/Appointments/Details/{appointmentNotification.AppointmentId}";
                
                var replacements = new Dictionary<string, string>
                {
                    { "DoctorName", string.IsNullOrEmpty(appointmentNotification.DoctorName) ? "Doctor" : appointmentNotification.DoctorName },
                    { "PatientName", string.IsNullOrEmpty(appointmentNotification.PatientName) ? "Patient" : appointmentNotification.PatientName },
                    { "PatientEmail", string.IsNullOrEmpty(appointmentNotification.PatientEmail) ? "N/A" : appointmentNotification.PatientEmail },
                    { "PatientPhone", string.IsNullOrEmpty(appointmentNotification.PatientPhoneNumber) ? "N/A" : appointmentNotification.PatientPhoneNumber },
                    { "AppointmentId", appointmentNotification.AppointmentId.ToString() },
                    { "AppointmentDate", appointmentNotification.FormattedAppointmentDate },
                    { "AppointmentTime", appointmentNotification.FormattedAppointmentTime },
                    { "TreatmentName", appointmentNotification.TreatmentName },
                    { "TreatmentDuration", appointmentNotification.TreatmentDuration.ToString() },
                    { "AppointmentDetailsUrl", appointmentDetailsUrl },
                    { "CurrentYear", DateTime.Now.Year.ToString() }
                };
                
                string emailContent = await _templateService.GetEmailTemplateAsync("DoctorAppointment", replacements);
                
                if (!string.IsNullOrEmpty(emailContent))
                {
                    await SendEmailAsync(appointmentNotification.DoctorEmail, "New Appointment Notification - SmileCraft Dental", emailContent);
                    _logger.LogInformation($"Sent appointment notification email to Dr. {appointmentNotification.DoctorName} for appointment {appointmentNotification.AppointmentId}");
                }
                else
                {
                    _logger.LogWarning($"Failed to generate doctor appointment notification email content for {appointmentNotification.DoctorEmail}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending doctor appointment notification email to {appointmentNotification.DoctorEmail}");
            }
        }
    }
}