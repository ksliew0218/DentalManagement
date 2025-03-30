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
                else
                {
                    // Fallback to old template if the new one fails
                    var oldTemplate = GetEmailConfirmationTemplate(firstName, encodedUrl);
                    await SendEmailAsync(email, "Confirm Your SmileCraft Account", oldTemplate);
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

        // Keep the old template method for fallback
        private string GetEmailConfirmationTemplate(string firstName, string callbackUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Confirm Your SmileCraft Account</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Poppins:wght@400;600&family=Outfit:wght@400;500;700&display=swap');
        
        body {{
            font-family: 'Poppins', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f9f9f9;
        }}
        .email-container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.05);
        }}
        .email-header {{
            background-color: #ffffff;
            padding: 30px 24px;
            text-align: center;
            border-bottom: 1px solid #f0f0f0;
        }}
        .logo-container {{
            display: flex;
            align-items: center;
            justify-content: center;
            margin: 0 auto;
        }}
        .logo-icon {{
            color: #C2D8D9;
            font-size: 24px;
            margin-right: 10px;
            display: inline-flex;
            align-items: center;
        }}
        .logo-text {{
            font-family: 'Outfit', sans-serif;
            font-weight: 500;
            font-size: 30px;
            color: #333;
            margin: 0;
            padding: 0;
            line-height: 1;
        }}
        .email-body {{
            padding: 32px 24px;
        }}
        .greeting {{
            font-size: 22px;
            font-weight: 600;
            margin-bottom: 16px;
            color: #2d3748;
        }}
        .message {{
            font-size: 16px;
            margin-bottom: 24px;
            color: #4a5568;
        }}
        .cta-button {{
            display: inline-block;
            background-color: #333333;
            color: #ffffff !important;
            text-decoration: none;
            font-weight: 600;
            font-size: 16px;
            padding: 12px 32px;
            border-radius: 4px;
            margin: 16px 0 24px;
            text-align: center;
            transition: all 0.3s ease;
        }}
        .cta-button:hover {{
            background-color: #000000;
        }}
        .guidance {{
            background-color: #f8f8f8;
            padding: 16px;
            border-radius: 6px;
            margin-bottom: 24px;
            border-left: 3px solid #333333;
        }}
        .guidance-title {{
            font-weight: 600;
            margin-bottom: 8px;
            color: #2d3748;
        }}
        .help-text {{
            font-size: 14px;
            color: #64748b;
            margin-top: 24px;
            padding-top: 16px;
            border-top: 1px solid #e2e8f0;
        }}
        .email-footer {{
            background-color: #f8f8f8;
            padding: 16px 24px;
            text-align: center;
            font-size: 12px;
            color: #64748b;
            border-top: 1px solid #e2e8f0;
        }}
        @media only screen and (max-width: 600px) {{
            .email-body {{
                padding: 24px 16px;
            }}
            .logo-text {{
                font-size: 26px;
            }}
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='email-header'>
            <div class='logo-container'>
                <div class='logo-icon'>🦷</div>
                <div class='logo-text'>SmileCraft</div>
            </div>
        </div>
        <div class='email-body'>
            <div class='greeting'>Hello {(string.IsNullOrEmpty(firstName) ? "there" : firstName)},</div>
            <div class='message'>
                Thank you for signing up with SmileCraft Dental. To complete your registration and activate your account, please verify your email address by clicking the button below.
            </div>
            <div style='text-align: center;'>
                <a href='{callbackUrl}' class='cta-button'>Verify Email Address</a>
            </div>
            <div class='guidance'>
                <div class='guidance-title'>What happens next?</div>
                <ul style='padding-left: 20px; margin: 8px 0;'>
                    <li>After verification, you can log in to your account</li>
                    <li>Book appointments with our dental specialists</li>
                    <li>Manage your dental care all in one place</li>
                </ul>
            </div>
            <div class='message'>
                If you're having trouble with the button above, copy and paste the URL below into your web browser:
            </div>
            <div style='word-break: break-all; font-size: 14px; background-color: #f8f8f8; padding: 12px; border-radius: 4px; border: 1px solid #e2e8f0;'>
                {callbackUrl}
            </div>
            <div class='help-text'>
                If you didn't request this email, please ignore it or contact our support team if you have any concerns.
            </div>
        </div>
        <div class='email-footer'>
            <p>&copy; {DateTime.Now.Year} SmileCraft Dental. All rights reserved.</p>
            <p>123 Jalan Besar, Sri Petaling, Malaysia | +60 14-3281137</p>
        </div>
    </div>
</body>
</html>
";
        }
    }
}