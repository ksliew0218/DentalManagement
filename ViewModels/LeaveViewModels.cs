using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DentalManagement.Models;

namespace DentalManagement.ViewModels
{
    public class DoctorLeaveViewModel
    {
        public Doctor Doctor { get; set; }
        public List<DoctorLeaveBalance> LeaveBalances { get; set; } = new List<DoctorLeaveBalance>();
        public List<DoctorLeaveRequest> LeaveRequests { get; set; } = new List<DoctorLeaveRequest>();
    }
    
    public class LeaveRequestViewModel
    {
        public int DoctorId { get; set; }
        
        [Required]
        [Display(Name = "Leave Type")]
        public int LeaveTypeId { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }
        
        [StringLength(500)]
        [Display(Name = "Reason for Leave")]
        public string Reason { get; set; }
    }
    
    public class AdminLeaveManagementViewModel
    {
        public List<DoctorLeaveRequest> PendingRequests { get; set; }
        public List<DoctorLeaveRequest> ApprovedRequests { get; set; }
        public List<DoctorLeaveRequest> RejectedRequests { get; set; }
    }
    
    public class LeaveApprovalViewModel
    {
        public int LeaveRequestId { get; set; }
        public LeaveRequestStatus Status { get; set; }
        
        [StringLength(500)]
        [Display(Name = "Comments")]
        public string Comments { get; set; }
    }
} 