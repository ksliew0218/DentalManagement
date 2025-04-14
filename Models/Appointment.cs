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

        [Required]
        public int PatientId { get; set; }
        
        [ForeignKey(nameof(PatientId))]
        public virtual Patient? Patient { get; set; }

        [Required]
        public int DoctorId { get; set; }
        
        [ForeignKey(nameof(DoctorId))]
        public virtual Doctor? Doctor { get; set; }

        [Required]
        public int TreatmentTypeId { get; set; }
        
        [ForeignKey(nameof(TreatmentTypeId))]
        public virtual TreatmentType? TreatmentType { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public TimeSpan AppointmentTime { get; set; }
        
        public int Duration { get; set; } = 60;

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Confirmed";

        [Required]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }

        [StringLength(255)]
        public string? PaymentIntentId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }
        
        public virtual ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
        
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        
        public virtual ICollection<AppointmentReminder> Reminders { get; set; } = new List<AppointmentReminder>();

        public virtual ICollection<TreatmentReport> TreatmentReports { get; set; } = new List<TreatmentReport>();

        [NotMapped]
        public DateTime AppointmentDateTime => AppointmentDate.Date + AppointmentTime;

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

        public bool CanBeCancelled()
        {
            return Status != "Cancelled" && 
                   Status != "Completed" && 
                   (AppointmentDate > DateTime.Today || 
                    (AppointmentDate == DateTime.Today && 
                     AppointmentTime > DateTime.Now.TimeOfDay));
        }

        public bool IsEligibleForRefund()
        {
            DateTime appointmentDateTime = AppointmentDate.Date + AppointmentTime;
            DateTime now = DateTime.UtcNow;
            
            if (PaymentStatus != PaymentStatus.Paid && PaymentStatus != PaymentStatus.PartiallyPaid)
                return false;
                
            return (appointmentDateTime - now).TotalHours > 24;
        }
    }
}