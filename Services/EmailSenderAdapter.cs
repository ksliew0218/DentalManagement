using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace DentalManagement.Services
{
    public class EmailSenderAdapter : IEmailSender
    {
        private readonly IEmailService _emailService;

        public EmailSenderAdapter(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return _emailService.SendEmailAsync(email, subject, htmlMessage);
        }
    }
}