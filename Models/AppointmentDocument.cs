using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public class AppointmentDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        [ForeignKey("Appointment")]
        public int AppointmentId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string DocumentName { get; set; }
        
        [Required]
        [StringLength(500)]
        public string S3Key { get; set; }
        
        [StringLength(200)]
        public string ContentType { get; set; }
        
        public long FileSize { get; set; }
        
        [Required]
        public DateTime UploadedDate { get; set; }
        
        [StringLength(500)]
        public string UploadedBy { get; set; }
        
        // Navigation property
        public virtual Appointment Appointment { get; set; }
    }
} 