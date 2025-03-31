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
        
        // Appointment Foreign Key
        [Required]
        public int AppointmentId { get; set; }
        
        [ForeignKey(nameof(AppointmentId))]
        public virtual Appointment? Appointment { get; set; }
        
        // Payment Amount
        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        // Payment Type (Deposit, Full, Refund)
        [Required]
        public PaymentType PaymentType { get; set; }
        
        // Stripe-related fields
        [StringLength(255)]
        public string? PaymentMethodId { get; set; }
        
        [StringLength(255)]
        public string? PaymentIntentId { get; set; }
        
        [StringLength(255)]
        public string? CheckoutSessionId { get; set; }
        
        [StringLength(255)]
        public string? RefundId { get; set; }
        
        // Payment Status from Stripe
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "pending";
        
        // Error message if payment failed
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
        
        // Receipt URL from Stripe
        [StringLength(255)]
        public string? ReceiptUrl { get; set; }
        
        // Tracking fields
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }
        
        // User who processed the payment (if admin processed it)
        [StringLength(450)] // Match with AspNetUsers Id length
        public string? ProcessedByUserId { get; set; }
        
        // Additional notes about the payment
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}