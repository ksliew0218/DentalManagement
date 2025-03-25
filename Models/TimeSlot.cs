using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public class TimeSlot
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Doctor")]
        public int DoctorId { get; set; }
        
        // Make navigation property nullable to avoid null reference exceptions
        public virtual Doctor? Doctor { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        public bool IsBooked { get; set; } = false;
    }
} 