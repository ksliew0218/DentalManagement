using System;
using System.Threading.Tasks;
using DentalManagement.Models;
using DentalManagement.Services;
using DentalManagement.Areas.Patient.Models;
using Microsoft.AspNetCore.Authorization;
using DentalManagement.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalManagement.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize]
    [PatientOnly]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<NotificationsController> _logger;
        private readonly INotificationService _notificationService;

        public NotificationsController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<NotificationsController> logger,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(user.Id, 50);
            
            var preferences = await GetUserNotificationPreferencesAsync(user.Id);

            var viewModel = new NotificationsViewModel
            {
                Notifications = notifications,
                NotificationPreferences = preferences,
                UnreadCount = await _notificationService.GetUnreadCountAsync(user.Id)
            };

            return PartialView("_Notifications", viewModel);
        }

        private async Task<UserNotificationPreferences> GetUserNotificationPreferencesAsync(string userId)
        {
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
            }
            
            return preferences;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Patient/Notifications/SavePreferences")]
        public async Task<IActionResult> SavePreferences(UserNotificationPreferences model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found in SavePreferences");
                return NotFound();
            }

            _logger.LogInformation($"SavePreferences called for user {user.Id}");
            _logger.LogInformation($"Parameters: EmailAppointmentReminders={model.EmailAppointmentReminders}, " +
                               $"EmailNewAppointments={model.EmailNewAppointments}, " +
                               $"EmailAppointmentChanges={model.EmailAppointmentChanges}, " +
                               $"EmailPromotions={model.EmailPromotions}");

            try 
            {
                var preferences = await _context.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);

                if (preferences == null)
                {
                    preferences = new UserNotificationPreferences
                    {
                        UserId = user.Id,
                        EmailAppointmentReminders = model.EmailAppointmentReminders,
                        EmailNewAppointments = model.EmailNewAppointments,
                        EmailAppointmentChanges = model.EmailAppointmentChanges,
                        EmailPromotions = model.EmailPromotions,
                        LastUpdated = DateTime.UtcNow
                    };

                    _context.UserNotificationPreferences.Add(preferences);
                    _logger.LogInformation($"Created new notification preferences for user {user.Id}");
                }
                else
                {
                    preferences.EmailAppointmentReminders = model.EmailAppointmentReminders;
                    preferences.EmailNewAppointments = model.EmailNewAppointments;
                    preferences.EmailAppointmentChanges = model.EmailAppointmentChanges;
                    preferences.EmailPromotions = model.EmailPromotions;
                    preferences.LastUpdated = DateTime.UtcNow;

                    _logger.LogInformation($"Updated existing notification preferences for user {user.Id}");
                }

                var saveResult = await _context.SaveChangesAsync();
                _logger.LogInformation($"SaveChanges result: {saveResult} rows affected");

                TempData["SuccessMessage"] = "Your notification preferences have been updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification preferences for user {UserId}", user.Id);
                TempData["ErrorMessage"] = $"An error occurred while saving your preferences: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            await _notificationService.MarkAsReadAsync(id, user.Id);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            await _notificationService.MarkAllAsReadAsync(user.Id);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            await _notificationService.DeleteNotificationAsync(id, user.Id);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAll()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            await _notificationService.DeleteAllNotificationsAsync(user.Id);
            
            TempData["SuccessMessage"] = "All notifications have been cleared.";
            
            return RedirectToAction(nameof(Index));
        }
    }
}