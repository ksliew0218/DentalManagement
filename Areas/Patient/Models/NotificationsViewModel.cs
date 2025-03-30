using System.Collections.Generic;
using DentalManagement.Models;

namespace DentalManagement.Areas.Patient.Models
{
    public class NotificationsViewModel
    {
        public List<AppointmentReminder> AppointmentReminders { get; set; } = new List<AppointmentReminder>();
        public List<Appointment> AppointmentChanges { get; set; } = new List<Appointment>();
        public List<UserNotification> Notifications { get; set; } = new List<UserNotification>();
        public UserNotificationPreferences NotificationPreferences { get; set; }
        public int UnreadCount { get; set; }

        // Helper method to get the total count of all notification types
        public int TotalNotificationCount => 
            AppointmentReminders.Count + 
            AppointmentChanges.Count +
            Notifications.Count;
            
        // Check if there are any notifications
        public bool HasNotifications => 
            Notifications.Count > 0 || 
            AppointmentReminders.Count > 0 || 
            AppointmentChanges.Count > 0;
    }
}