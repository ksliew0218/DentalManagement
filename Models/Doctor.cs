using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public enum GenderType { Male, Female, Other }
    public enum StatusType { Active, Inactive }

    public class Doctor
    {
        [Key]
        public int Id { get; set; }  

        [ForeignKey("User")]
        public string UserID { get; set; } 
        public User User { get; set; } 

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public GenderType Gender { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        public string? PhoneNumber { get; set; }

        [Required]
        public string Specialty { get; set; } 

        public string? Qualifications { get; set; }

        public int ExperienceYears { get; set; } 

        public string? AvailableTimeSlots { get; set; } 

        public string? ProfilePictureUrl { get; set; } 
        public bool IsDeleted { get; set; } = false;

        [Required]
        public StatusType Status { get; set; } 
        
        public List<DoctorTreatment> DoctorTreatments { get; set; } = new();
        public List<DoctorLeaveBalance> LeaveBalances { get; set; } = new();
        public List<DoctorLeaveRequest> LeaveRequests { get; set; } = new();
    }
}
