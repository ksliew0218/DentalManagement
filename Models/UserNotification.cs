using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public class UserNotification
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        [Required]
        [StringLength(50)]
        public string NotificationType { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; }
        
        [Required]
        public string Message { get; set; }
        
        // Optional reference to related entity (like appointment)
        public int? RelatedEntityId { get; set; }
        
        // The controller and action for the View Details link
        [StringLength(50)]
        public string ActionController { get; set; }
        
        [StringLength(50)]
        public string ActionName { get; set; }
        
        // Read status
        [Required]
        public bool IsRead { get; set; }
        
        // Notification metadata
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ReadAt { get; set; }
        
        // Email tracking
        public bool EmailSent { get; set; }
        
        public DateTime? EmailSentAt { get; set; }
        
        // SMS tracking (for future use)
        public bool SmsSent { get; set; }
        
        public DateTime? SmsSentAt { get; set; }
    }
}