using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    [Table("TreatmentTypes")] // This explicitly names the table, but is optional
    public class TreatmentType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Use decimal for monetary values
        public decimal Price { get; set; }

        // Duration in minutes
        public int Duration { get; set; }

        // Image URL for treatment
        public string? ImageUrl { get; set; }

        // Mark active/inactive if you want to hide old treatments
        public bool IsActive { get; set; } = true;
        
        // Soft delete flag
        public bool IsDeleted { get; set; } = false;

        // Automatically set to UTC now upon creation
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public List<DoctorTreatment> DoctorTreatments { get; set; } = new();
    }
}