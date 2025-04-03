using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public class TreatmentReport
    {
        [Key]
        public int Id { get; set; }
        
        // Appointment Foreign Key
        [Required]
        public int AppointmentId { get; set; }
        
        [ForeignKey(nameof(AppointmentId))]
        public virtual Appointment? Appointment { get; set; }
        
        // Doctor Foreign Key
        [Required]
        public int DoctorId { get; set; }
        
        [ForeignKey(nameof(DoctorId))]
        public virtual Doctor? Doctor { get; set; }
        
        // Patient Foreign Key
        [Required]
        public int PatientId { get; set; }
        
        [ForeignKey(nameof(PatientId))]
        public virtual Patient? Patient { get; set; }
        
        // Treatment Date
        [Required]
        [DataType(DataType.Date)]
        public DateTime TreatmentDate { get; set; }
        
        // Treatment Notes
        [StringLength(2000)]
        public string? Notes { get; set; }
        
        // Dental Chart JSON 
        [Column(TypeName = "text")]
        public string? DentalChart { get; set; }
        
        // Tracking fields
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }
    }
} 