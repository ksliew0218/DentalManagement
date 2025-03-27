using System.ComponentModel.DataAnnotations;

namespace DentalManagement.Models
{
    public class LeaveType
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [Required]
        public bool IsPaid { get; set; }
        
        [Required]
        public int DefaultDays { get; set; }
        
        [StringLength(255)]
        public string Description { get; set; }
    }
} 