using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace DentalManagement.Services
{
    public class EmailTemplateService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EmailTemplateService> _logger;

        public EmailTemplateService(IWebHostEnvironment environment, ILogger<EmailTemplateService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> GetTemplateAsync(string templateName)
        {
            try
            {
                string templatePath = Path.Combine(_environment.ContentRootPath, "EmailTemplates", templateName, "template.html");
                if (!File.Exists(templatePath))
                {
                    _logger.LogWarning($"Email template not found: {templatePath}");
                    return string.Empty;
                }
                return await File.ReadAllTextAsync(templatePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading email template: {templateName}");
                return string.Empty;
            }
        }

        public async Task<string> GetStylesAsync(string templateName)
        {
            try
            {
                string stylesPath = Path.Combine(_environment.ContentRootPath, "EmailTemplates", templateName, "styles.css");
                if (!File.Exists(stylesPath))
                {
                    _logger.LogWarning($"Email styles not found: {stylesPath}");
                    return string.Empty;
                }
                return await File.ReadAllTextAsync(stylesPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading email styles: {templateName}");
                return string.Empty;
            }
        }

        public async Task<string> GetEmailTemplateAsync(string templateName, Dictionary<string, string> replacements)
        {
            try
            {
                string template = await GetTemplateAsync(templateName);
                if (string.IsNullOrEmpty(template))
                {
                    return string.Empty;
                }

                string styles = await GetStylesAsync(templateName);
                
                template = template.Replace("{{styles}}", styles);
                
                foreach (var replacement in replacements)
                {
                    template = template.Replace($"{{{{{replacement.Key}}}}}", replacement.Value);
                }
                
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error preparing email template: {templateName}");
                return string.Empty;
            }
        }
    }
}