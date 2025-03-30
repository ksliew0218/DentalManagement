using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public class AppointmentReminder
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int AppointmentId { get; set; }
        
        [ForeignKey("AppointmentId")]
        public Appointment Appointment { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ReminderType { get; set; }
        
        [Required]
        public DateTime SentAt { get; set; }
        
        // Optional tracking for SMS reminders
        public bool SentBySMS { get; set; }
        
        // Optional tracking for push notifications
        public bool SentByPushNotification { get; set; }
    }
}