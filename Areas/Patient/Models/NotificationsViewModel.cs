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

        public int TotalNotificationCount => 
            AppointmentReminders.Count + 
            AppointmentChanges.Count +
            Notifications.Count;
            
        public bool HasNotifications => 
            Notifications.Count > 0 || 
            AppointmentReminders.Count > 0 || 
            AppointmentChanges.Count > 0;
    }
}