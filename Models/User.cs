using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace DentalManagement.Models
{
    public enum UserRole
    {
        Admin,
        Doctor,
        Patient
    }

    public class User : IdentityUser // ✅ Identity 需要继承 IdentityUser
    {
        public UserRole Role { get; set; } = UserRole.Patient;
        public bool IsActive { get; set; } = true;

        private DateTime _createdAt = DateTime.UtcNow;
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => _createdAt = DateTime.SpecifyKind(value, DateTimeKind.Utc); // ✅ 确保 UTC
        }

        private DateTime _updatedAt = DateTime.UtcNow;
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => _updatedAt = DateTime.SpecifyKind(value, DateTimeKind.Utc); // ✅ 确保 UTC
        }

        // 🔹 关联 Patients
        public virtual ICollection<Patient> Patients { get; set; }
    }
}
