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

        // GET: Patient/Notifications
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get the user's notifications
            var notifications = await _notificationService.GetUserNotificationsAsync(user.Id, 50);
            
            // Get the user's notification preferences
            var preferences = await GetUserNotificationPreferencesAsync(user.Id);

            // Create view model
            var viewModel = new NotificationsViewModel
            {
                Notifications = notifications,
                NotificationPreferences = preferences,
                UnreadCount = await _notificationService.GetUnreadCountAsync(user.Id)
            };

            return PartialView("_Notifications", viewModel);
        }

        // Helper method to get or create user notification preferences
        private async Task<UserNotificationPreferences> GetUserNotificationPreferencesAsync(string userId)
        {
            // Get current preferences or create new default preferences
            var preferences = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (preferences == null)
            {
                // Create default preferences for new users
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
                // Find existing preferences
                var preferences = await _context.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);

                if (preferences == null)
                {
                    // Create new preferences
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
                    // Update existing preferences
                    preferences.EmailAppointmentReminders = model.EmailAppointmentReminders;
                    preferences.EmailNewAppointments = model.EmailNewAppointments;
                    preferences.EmailAppointmentChanges = model.EmailAppointmentChanges;
                    preferences.EmailPromotions = model.EmailPromotions;
                    preferences.LastUpdated = DateTime.UtcNow;

                    _logger.LogInformation($"Updated existing notification preferences for user {user.Id}");
                }

                // Save changes with error tracking
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

        // POST: Patient/Notifications/MarkAsRead
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
            
            // If the request is AJAX, return success
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Patient/Notifications/MarkAllAsRead
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
            
            // If the request is AJAX, return success
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Patient/Notifications/Delete
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
            
            // If the request is AJAX, return success
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Patient/Notifications/ClearAll
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