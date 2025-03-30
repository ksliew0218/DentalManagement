using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalManagement.Services
{
    public interface INotificationService
    {
        Task<UserNotification> CreateNotificationAsync(
            string userId,
            string notificationType,
            string title,
            string message,
            int? relatedEntityId = null,
            string? actionController = null,
            string? actionName = null);
            
        Task<UserNotification> CreateAppointmentNotificationAsync(
            string userId,
            int appointmentId,
            string notificationType,
            string title,
            string message);
            
        Task<bool> MarkAsReadAsync(int notificationId, string userId);
        
        Task<bool> MarkAllAsReadAsync(string userId);
        
        Task<bool> DeleteNotificationAsync(int notificationId, string userId);
        
        Task<bool> DeleteAllNotificationsAsync(string userId);
        
        Task<List<UserNotification>> GetUserNotificationsAsync(
            string userId, 
            int count = 20, 
            bool unreadOnly = false);
            
        Task<int> GetUnreadCountAsync(string userId);
    }
    
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        
        public NotificationService(
            ApplicationDbContext context,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<UserNotification> CreateNotificationAsync(
            string userId,
            string notificationType,
            string title,
            string message,
            int? relatedEntityId = null,
            string? actionController = null,
            string? actionName = null)
        {
            try
            {
                var notification = new UserNotification
                {
                    UserId = userId,
                    NotificationType = notificationType,
                    Title = title,
                    Message = message,
                    RelatedEntityId = relatedEntityId,
                    ActionController = actionController,
                    ActionName = actionName,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.UserNotifications.Add(notification);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created notification ID {NotificationId} for user {UserId}", 
                    notification.Id, userId);
                    
                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", userId);
                throw;
            }
        }
        
        public async Task<UserNotification> CreateAppointmentNotificationAsync(
            string userId,
            int appointmentId,
            string notificationType,
            string title,
            string message)
        {
            return await CreateNotificationAsync(
                userId,
                notificationType,
                title,
                message,
                relatedEntityId: appointmentId,
                actionController: "Appointments",
                actionName: "Details");
        }
        
        public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
        {
            try
            {
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
                    
                if (notification == null)
                {
                    _logger.LogWarning("Notification ID {NotificationId} not found for user {UserId}", 
                        notificationId, userId);
                    return false;
                }
                
                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Marked notification ID {NotificationId} as read for user {UserId}", 
                        notificationId, userId);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", 
                    notificationId, userId);
                return false;
            }
        }
        
        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            try
            {
                var unreadNotifications = await _context.UserNotifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();
                    
                if (!unreadNotifications.Any())
                {
                    return true; // No unread notifications
                }
                
                var now = DateTime.UtcNow;
                
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = now;
                }
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Marked all notifications as read for user {UserId} (Count: {Count})", 
                    userId, unreadNotifications.Count);
                    
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                return false;
            }
        }
        
        public async Task<bool> DeleteNotificationAsync(int notificationId, string userId)
        {
            try
            {
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
                    
                if (notification == null)
                {
                    _logger.LogWarning("Notification ID {NotificationId} not found for user {UserId} when attempting to delete", 
                        notificationId, userId);
                    return false;
                }
                
                _context.UserNotifications.Remove(notification);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted notification ID {NotificationId} for user {UserId}", 
                    notificationId, userId);
                    
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", 
                    notificationId, userId);
                return false;
            }
        }
        
        public async Task<bool> DeleteAllNotificationsAsync(string userId)
        {
            try
            {
                var notifications = await _context.UserNotifications
                    .Where(n => n.UserId == userId)
                    .ToListAsync();
                    
                if (!notifications.Any())
                {
                    return true; // No notifications to delete
                }
                
                _context.UserNotifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted all notifications for user {UserId} (Count: {Count})", 
                    userId, notifications.Count);
                    
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all notifications for user {UserId}", userId);
                return false;
            }
        }
        
        public async Task<List<UserNotification>> GetUserNotificationsAsync(
            string userId, 
            int count = 20, 
            bool unreadOnly = false)
        {
            try
            {
                var query = _context.UserNotifications
                    .Where(n => n.UserId == userId);
                    
                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }
                
                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(count)
                    .ToListAsync();
                    
                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                return new List<UserNotification>();
            }
        }
        
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            try
            {
                return await _context.UserNotifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                return 0;
            }
        }
    }
}