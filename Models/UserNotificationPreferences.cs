using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public class UserNotificationPreferences
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        // Email notification preferences
        [Required]
        [Display(Name = "Email Appointment Reminders")]
        public bool EmailAppointmentReminders { get; set; } = true;
        
        [Required]
        [Display(Name = "Email for New Appointments")]
        public bool EmailNewAppointments { get; set; } = true;
        
        [Required]
        [Display(Name = "Email for Appointment Changes")]
        public bool EmailAppointmentChanges { get; set; } = true;
        
        [Required]
        [Display(Name = "Email for Promotions and News")]
        public bool EmailPromotions { get; set; } = true;
        
        // Timing preferences
        [Required]
        [Display(Name = "24-Hour Reminder")]
        public bool Want24HourReminder { get; set; } = true;
        
        [Required]
        [Display(Name = "48-Hour Reminder")]
        public bool Want48HourReminder { get; set; } = true;
        
        [Required]
        [Display(Name = "1-Week Reminder")]
        public bool WantWeekReminder { get; set; } = true;
        
        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}