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

        private DateTime _startTime;
        
        [Required]
        public DateTime StartTime
        {
            get => _startTime;
            set => _startTime = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private DateTime _endTime;
        
        [Required]
        public DateTime EndTime
        {
            get => _endTime;
            set => _endTime = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        [Required]
        public bool IsBooked { get; set; } = false;
    }
} 