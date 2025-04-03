using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public enum PaymentStatus
    {
        Pending,
        Paid,
        PartiallyPaid,
        Refunded,
        PartiallyRefunded,
        Failed,
        Cancelled
    }

    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        // Patient Foreign Key
        [Required]
        public int PatientId { get; set; }
        
        [ForeignKey(nameof(PatientId))]
        public virtual Patient? Patient { get; set; }

        // Doctor Foreign Key
        [Required]
        public int DoctorId { get; set; }
        
        [ForeignKey(nameof(DoctorId))]
        public virtual Doctor? Doctor { get; set; }

        // Treatment Type Foreign Key
        [Required]
        public int TreatmentTypeId { get; set; }
        
        [ForeignKey(nameof(TreatmentTypeId))]
        public virtual TreatmentType? TreatmentType { get; set; }

        // Appointment Details
        [Required]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public TimeSpan AppointmentTime { get; set; }
        
        // Duration in minutes - derive from treatment
        public int Duration { get; set; } = 60;

        // Optional notes from patient or doctor
        [StringLength(500)]
        public string? Notes { get; set; }

        // Appointment Status
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Scheduled";

        // Payment related fields
        [Required]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }

        // Stripe Payment Intent ID
        [StringLength(255)]
        public string? PaymentIntentId { get; set; }

        // Tracking fields
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }
        
        // Collection of TimeSlots used for this appointment
        public virtual ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
        
        // Collection of payments related to this appointment
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        
        // Add this collection property for reminders
        public virtual ICollection<AppointmentReminder> Reminders { get; set; } = new List<AppointmentReminder>();

        // Collection of treatment reports
        public virtual ICollection<TreatmentReport> TreatmentReports { get; set; } = new List<TreatmentReport>();

        // Calculated property for appointment datetime
        [NotMapped]
        public DateTime AppointmentDateTime => AppointmentDate.Date + AppointmentTime;

        // Validation for appointment status
        public bool IsValidStatus()
        {
            string[] validStatuses = { 
                "Scheduled", 
                "Confirmed", 
                "Completed", 
                "Cancelled", 
                "Rescheduled", 
                "No-Show" 
            };

            return Array.Exists(validStatuses, s => s == Status);
        }

        // Method to check if appointment can be cancelled
        public bool CanBeCancelled()
        {
            return Status != "Cancelled" && 
                   Status != "Completed" && 
                   (AppointmentDate > DateTime.Today || 
                    (AppointmentDate == DateTime.Today && 
                     AppointmentTime > DateTime.Now.TimeOfDay));
        }

        // Method to check if appointment is eligible for refund
        public bool IsEligibleForRefund()
        {
            // Check if the appointment is more than 24 hours away
            DateTime appointmentDateTime = AppointmentDate.Date + AppointmentTime;
            DateTime now = DateTime.UtcNow;
            
            // If payment status is not Paid or PartiallyPaid, not eligible for refund
            if (PaymentStatus != PaymentStatus.Paid && PaymentStatus != PaymentStatus.PartiallyPaid)
                return false;
                
            // Eligible for refund if more than 24 hours before appointment
            return (appointmentDateTime - now).TotalHours > 24;
        }
    }
}