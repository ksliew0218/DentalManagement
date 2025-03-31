using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DentalManagement.Areas.Patient.Models;
using DentalManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DentalManagement.Services
{
    public class AppointmentReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentReminderService> _logger;
        private readonly TimeSpan _checkInterval;

        public AppointmentReminderService(
            IServiceScopeFactory scopeFactory,
            ILogger<AppointmentReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            
            // TEMPORARY: Check for reminders every 1 minute for testing
            // _checkInterval = TimeSpan.FromMinutes(1);
            
            // Original setting (restore after testing):
            _checkInterval = TimeSpan.FromHours(1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("⭐⭐⭐ Appointment Reminder Service is starting at {time} ⭐⭐⭐", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking for appointment reminders at: {time}", DateTimeOffset.Now);
                
                try
                {
                    _logger.LogInformation("Running initial reminder check at startup");
                    await ProcessRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing appointment reminders");
                }

                // Wait for the next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Appointment Reminder Service is stopping");
        }

        private async Task ProcessRemindersAsync(CancellationToken stoppingToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // TEMPORARY: For testing, we'll look for appointments coming up in the next 2 days
                // regardless of the exact time
                var now = DateTime.UtcNow;
                _logger.LogInformation($"Current time (UTC): {now}");
                
                // Only use the 48-hour reminder period
                var targetDate = now.AddDays(2).Date;
                _logger.LogInformation($"Looking for appointments on target date: {targetDate.ToShortDateString()}");
                
                var reminderType = "48hour";
                var templateName = "Appointment48HourReminder";
                
                if (!stoppingToken.IsCancellationRequested)
                {
                    // Get all appointments for the target date that haven't been reminded yet
                    var appointments = await dbContext.Appointments
                        .Include(a => a.Patient)
                            .ThenInclude(p => p.User)
                        .Include(a => a.Doctor)
                        .Include(a => a.TreatmentType)
                        .Include(a => a.Reminders)
                        .Where(a => 
                            a.AppointmentDate.Date == targetDate &&
                            a.Status != "Cancelled" &&
                            !a.Reminders.Any(r => r.ReminderType == reminderType))
                        .ToListAsync();
                        
                    _logger.LogInformation($"Found {appointments.Count} appointments for {targetDate.ToShortDateString()} that need 48-hour reminders");
                    
                    foreach (var appointment in appointments)
                    {
                        try
                        {
                            // Add record to track that we've processed this appointment for this reminder type
                            var reminderRecord = new AppointmentReminder
                            {
                                AppointmentId = appointment.Id,
                                ReminderType = reminderType,
                                SentAt = now,
                                SentByEmail = false,
                                SentByPushNotification = false
                            };
                            
                            _logger.LogInformation($"Processing appointment ID: {appointment.Id} for {appointment.Patient.FirstName} {appointment.Patient.LastName} on {appointment.AppointmentDate.ToShortDateString()} at {appointment.AppointmentTime}");
                            
                            dbContext.AppointmentReminders.Add(reminderRecord);
                            await dbContext.SaveChangesAsync();
                            
                            // Get user ID
                            var userId = appointment.Patient.UserID;
                            
                            // Get the user's notification preferences
                            var preferences = await dbContext.UserNotificationPreferences
                                .FirstOrDefaultAsync(p => p.UserId == userId);
                            
                            // If no preferences found, create default ones
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
                                
                                dbContext.UserNotificationPreferences.Add(preferences);
                                await dbContext.SaveChangesAsync();
                            }
                            
                            // Format appointment date and time for notifications
                            string formattedDate = appointment.AppointmentDate.ToString("dddd, MMMM d, yyyy");
                            
                            // Format time
                            bool isPM = appointment.AppointmentTime.Hours >= 12;
                            int hour12 = appointment.AppointmentTime.Hours % 12;
                            if (hour12 == 0) hour12 = 12;
                            string formattedTime = $"{hour12}:{appointment.AppointmentTime.Minutes:D2} {(isPM ? "PM" : "AM")}";
                            
                            // Get remaining fields needed for the notification
                            string doctorName = $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}";
                            string treatmentName = appointment.TreatmentType.Name;
                            
                            // Create in-app notification (always create regardless of email preference)
                            string title = "Appointment Reminder: 48-hour";
                            string message = $"Your {treatmentName} appointment with {doctorName} is scheduled for {formattedDate} at {formattedTime}.";
                            
                            var notification = await notificationService.CreateAppointmentNotificationAsync(
                                userId,
                                appointment.Id,
                                $"AppointmentReminder_48hour",
                                title,
                                message);
                            
                            _logger.LogInformation($"Created in-app notification for appointment ID: {appointment.Id}");
                            
                            // Track that in-app notification was created
                            reminderRecord.SentByPushNotification = true;
                            
                            // Check if user wants email reminders
                            if (preferences.EmailAppointmentReminders)
                            {
                                // Create appointment details view model for the email
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
                                await emailService.SendAppointmentReminderEmailAsync(
                                    patientEmail, 
                                    patientName, 
                                    appointmentDetails, 
                                    "48-hour",
                                    templateName);
                                
                                _logger.LogInformation($"Sent 48-hour reminder email for appointment ID: {appointment.Id} to {patientEmail}");
                                
                                // Track that the email was sent
                                reminderRecord.SentByEmail = true;
                                    
                                // Update the notification to track email sent
                                if (notification != null)
                                {
                                    notification.EmailSent = true;
                                    notification.EmailSentAt = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"Skipping email for 48-hour reminder for appointment ID: {appointment.Id} because email reminders are disabled");
                            }
                            
                            // Save changes to the reminder record
                            await dbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending reminder for appointment ID {appointmentId}", appointment.Id);
                        }
                    }
                }
            }
        }
    }
}