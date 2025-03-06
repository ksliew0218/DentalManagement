using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public string UserID { get; set; }  // ✅ IdentityUser 的主键是 string
        public User User { get; set; }  // ✅ 关联 User 表

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Gender { get; set; }  // ENUM('Male', 'Female', 'Other')

        private DateTime _dateOfBirth;
        [Required]
        public DateTime DateOfBirth
        {
            get => _dateOfBirth;
            set => _dateOfBirth = DateTime.SpecifyKind(value, DateTimeKind.Utc); // ✅ 确保存储为 UTC
        }

        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }
}
