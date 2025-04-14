using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public class TreatmentReport
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int AppointmentId { get; set; }
        
        [ForeignKey(nameof(AppointmentId))]
        public virtual Appointment? Appointment { get; set; }
        
        [Required]
        public int DoctorId { get; set; }
        
        [ForeignKey(nameof(DoctorId))]
        public virtual Doctor? Doctor { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        [ForeignKey(nameof(PatientId))]
        public virtual Patient? Patient { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime TreatmentDate { get; set; }
        
        [StringLength(2000)]
        public string? Notes { get; set; }
        
        [Column(TypeName = "text")]
        public string? DentalChart { get; set; }
        
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }
    }
} 