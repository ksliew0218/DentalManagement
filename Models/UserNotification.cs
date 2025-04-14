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
    [StringLength(100)]
    public string NotificationType { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; }
    
    [Required]
    public string Message { get; set; }
    
    public int? RelatedEntityId { get; set; }
    
    public string ActionController { get; set; }
    
    public string ActionName { get; set; }
    
    [Required]
    public bool IsRead { get; set; } = false;
    
    public DateTime? ReadAt { get; set; }
    
    public bool EmailSent { get; set; } = false;
    
    public DateTime? EmailSentAt { get; set; }
    
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
}