using System;
using System.ComponentModel.DataAnnotations;

namespace DentalManagement.Models
{
    public enum UserRole
    {
        Doctor,
        Patient,
        Admin
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, EmailAddress]
        public required string Email { get; set; }  // 添加 required 关键字

        [Required, MinLength(4)]
        public required string Username { get; set; }  // 添加 required 关键字

        [Required]
        public required string Password { get; set; }  // 添加 required 关键字

        [Required]
        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
