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
        public int Id { get; set; }  // Primary Key (Auto Increment)

        [ForeignKey("User")]
        public string UserID { get; set; } // Foreign Key for User
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
        public string Specialty { get; set; } // Doctor's specialty (e.g., Orthodontist, Surgeon)

        public string? Qualifications { get; set; }

        public int ExperienceYears { get; set; } // Years of experience

        public string? AvailableTimeSlots { get; set; } // Store JSON data for time slots

        public string? ProfilePictureUrl { get; set; } // Store URL for profile picture

        [Required]
        public StatusType Status { get; set; } // Active/Inactive
        public List<DoctorTreatment> DoctorTreatments { get; set; } = new();
    }
}
