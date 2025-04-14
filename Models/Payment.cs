using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public enum PaymentType
    {
        Deposit,
        FullPayment,
        Refund
    }

    public class Payment
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int AppointmentId { get; set; }
        
        [ForeignKey(nameof(AppointmentId))]
        public virtual Appointment? Appointment { get; set; }
        
        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        public PaymentType PaymentType { get; set; }
        
        [StringLength(255)]
        public string? PaymentMethodId { get; set; }
        
        [StringLength(255)]
        public string? PaymentIntentId { get; set; }
        
        [StringLength(255)]
        public string? CheckoutSessionId { get; set; }
        
        [StringLength(255)]
        public string? RefundId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "pending";
        
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
        
        [StringLength(255)]
        public string? ReceiptUrl { get; set; }
    
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }
        
        [StringLength(450)] 
        public string? ProcessedByUserId { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}