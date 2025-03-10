using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalManagement.Models
{
    public class DoctorTreatment
    {
        [Key]
        public int Id { get; set; }  // Primary Key

        [ForeignKey("Doctor")]
        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }

        [ForeignKey("TreatmentType")]
        public int TreatmentTypeId { get; set; }
        public TreatmentType TreatmentType { get; set; }
    }
}