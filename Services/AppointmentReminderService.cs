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
            
            // Check for reminders every 1 hour by default
            // In a production environment, you might want to make this configurable
            _checkInterval = TimeSpan.FromHours(1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Appointment Reminder Service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking for appointment reminders at: {time}", DateTimeOffset.Now);
                
                try
                {
                    await ProcessRemindersAsync();
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

        private async Task ProcessRemindersAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // Get current date/time in UTC
                var now = DateTime.UtcNow;
                
                // Define reminder periods to check
                // For example: 24 hours, 48 hours, and 1 week before appointment
                var reminderPeriods = new[]
                {
                    new { Period = TimeSpan.FromHours(24), Template = "Appointment24HourReminder", Type = "24hour" },
                    new { Period = TimeSpan.FromDays(2), Template = "Appointment48HourReminder", Type = "48hour" },
                    new { Period = TimeSpan.FromDays(7), Template = "AppointmentWeekReminder", Type = "week" }
                };
                
                foreach (var reminderPeriod in reminderPeriods)
                {
                    await SendRemindersForPeriodAsync(
                        dbContext, 
                        emailService,
                        notificationService,
                        now, 
                        reminderPeriod.Period, 
                        reminderPeriod.Template,
                        reminderPeriod.Type);
                }
            }
        }

        private async Task SendRemindersForPeriodAsync(
            ApplicationDbContext dbContext, 
            IEmailService emailService,
            INotificationService notificationService,
            DateTime now, 
            TimeSpan reminderPeriod,
            string templateName,
            string reminderType)
        {
            // Calculate the target time window for reminders
            var targetTime = now + reminderPeriod;
            
            // Determine which notification preference to check
            Func<UserNotificationPreferences, bool> preferenceCheckFunc;
            
            if (reminderPeriod.TotalHours <= 24)
                preferenceCheckFunc = prefs => prefs.Want24HourReminder;
            else if (reminderPeriod.TotalDays <= 2)
                preferenceCheckFunc = prefs => prefs.Want48HourReminder;
            else if (reminderPeriod.TotalDays <= 7)
                preferenceCheckFunc = prefs => prefs.WantWeekReminder;
            else
                preferenceCheckFunc = _ => true; // Default to true for any other period
            
            // Get appointments that:
            // 1. Haven't been reminded for this period yet
            // 2. Are scheduled around the target time
            // 3. Are not cancelled
            var appointments = await dbContext.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                .Include(a => a.TreatmentType)
                .Include(a => a.Reminders)
                .Where(a => 
                    a.AppointmentDate.Date == targetTime.Date &&
                    a.AppointmentTime >= targetTime.TimeOfDay.Add(TimeSpan.FromMinutes(-30)) &&
                    a.AppointmentTime <= targetTime.TimeOfDay.Add(TimeSpan.FromMinutes(30)) &&
                    a.Status != "Cancelled" &&
                    !a.Reminders.Any(r => r.ReminderType == reminderType))
                .ToListAsync();
                
            _logger.LogInformation("Found {count} appointments for {reminderPeriod} reminder", 
                appointments.Count, reminderPeriod.ToString());
                
            foreach (var appointment in appointments)
            {
                try
                {
                    // Check user notification preferences
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
                            Want24HourReminder = true,
                            Want48HourReminder = true,
                            WantWeekReminder = true,
                            LastUpdated = DateTime.UtcNow
                        };
                        
                        dbContext.UserNotificationPreferences.Add(preferences);
                        await dbContext.SaveChangesAsync();
                    }
                    
                    // Check if user wants this type of reminder
                    bool wantsThisReminder = preferenceCheckFunc(preferences);
                    bool wantsEmailReminders = preferences.EmailAppointmentReminders;
                    
                    // Skip if user doesn't want this reminder or email notifications
                    if (!wantsThisReminder || !wantsEmailReminders)
                    {
                        _logger.LogInformation("Skipping reminder for appointment ID {appointmentId} based on user preferences", appointment.Id);
                        
                        // Still record that we processed this reminder, even if we didn't send it
                        var skippedReminder = new AppointmentReminder
                        {
                            AppointmentId = appointment.Id,
                            ReminderType = reminderType,
                            SentAt = now,
                            SentBySMS = false  // Not sent
                        };
                        
                        dbContext.AppointmentReminders.Add(skippedReminder);
                        await dbContext.SaveChangesAsync();
                        continue;
                    }
                    
                    // Create appointment details view model for the email
                    var appointmentDetails = new AppointmentDetailViewModel
                    {
                        Id = appointment.Id,
                        TreatmentName = appointment.TreatmentType.Name,
                        DoctorName = $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}",
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
                        GetReminderTypeDescription(reminderPeriod),
                        templateName);
                        
                    // Create in-app notification
                    string title = $"Appointment Reminder: {GetReminderTypeDescription(reminderPeriod)}";
                    string message = $"Your {appointment.TreatmentType.Name} appointment with Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName} is scheduled for {appointment.AppointmentDate.ToString("MMMM d")} at {appointment.AppointmentTime.ToString(@"hh\:mm tt")}.";
                    
                    await notificationService.CreateAppointmentNotificationAsync(
                        userId,
                        appointment.Id,
                        $"AppointmentReminder_{reminderType}",
                        title,
                        message);
                        
                    // Record that reminder was sent
                    var reminder = new AppointmentReminder
                    {
                        AppointmentId = appointment.Id,
                        ReminderType = reminderType,
                        SentAt = now,
                        SentBySMS = false
                    };
                    
                    dbContext.AppointmentReminders.Add(reminder);
                    await dbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("Sent {reminderType} reminder for appointment ID {appointmentId} to {email}",
                        reminderType, appointment.Id, patientEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending reminder for appointment ID {appointmentId}", appointment.Id);
                }
            }
        }
        
        private string GetReminderTypeDescription(TimeSpan period)
        {
            if (period.TotalHours <= 24)
                return "24-hour";
            if (period.TotalDays <= 2)
                return "48-hour";
            if (period.TotalDays <= 7)
                return "one-week";
            
            return $"{period.TotalDays}-day";
        }
    }
}