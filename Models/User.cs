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

    public class User : IdentityUser
    {
        public UserRole Role { get; set; } = UserRole.Patient;
        public bool IsActive { get; set; } = true;

        private DateTime _createdAt = DateTime.UtcNow;
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => _createdAt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private DateTime _updatedAt = DateTime.UtcNow;
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => _updatedAt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        public virtual ICollection<Patient> Patients { get; set; }
    }
}
