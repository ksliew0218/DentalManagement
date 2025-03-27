using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public enum LeaveRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class DoctorLeaveRequest
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [ForeignKey("Doctor")]
        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }
        
        [Required]
        [ForeignKey("LeaveType")]
        public int LeaveTypeId { get; set; }
        public LeaveType LeaveType { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [Required]
        public decimal TotalDays { get; set; }
        
        [StringLength(500)]
        public string Reason { get; set; }
        
        public string DocumentPath { get; set; } // Path to attached document if any
        
        [Required]
        public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;
        
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("ApprovedByUser")]
        public string ApprovedById { get; set; }
        public User ApprovedByUser { get; set; }
        
        public DateTime? ApprovalDate { get; set; }
        
        public string Comments { get; set; } // Comments from admin
    }
} 