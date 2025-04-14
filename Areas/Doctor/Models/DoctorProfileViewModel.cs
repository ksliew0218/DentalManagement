using System;
using System.ComponentModel.DataAnnotations;

namespace DentalManagement.Areas.Doctor.Models
{
    public class DoctorProfileViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Gender")]
        public string Gender { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Specialty")]
        public string Specialty { get; set; }

        [Display(Name = "Qualifications")]
        public string? Qualifications { get; set; }

        [Display(Name = "Years of Experience")]
        [Range(0, 100, ErrorMessage = "Experience years must be between 0 and 100")]
        public int ExperienceYears { get; set; }

        public string? ProfilePictureUrl { get; set; }
    }
} 