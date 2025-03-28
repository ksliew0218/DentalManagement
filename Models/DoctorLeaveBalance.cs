using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public class DoctorLeaveBalance
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
        public decimal RemainingDays { get; set; }
        
        [Required]
        public decimal TotalDays { get; set; } = 0; // Default allocation days for this leave type
        
        public int Year { get; set; } // To track leave balances by year
    }
} 