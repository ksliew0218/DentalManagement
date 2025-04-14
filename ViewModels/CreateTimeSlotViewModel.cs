using System;
using System.ComponentModel.DataAnnotations;

namespace DentalManagement.ViewModels
{
    public class CreateTimeSlotViewModel
    {
        [Required(ErrorMessage = "Doctor is required")]
        public int DoctorId { get; set; }
        
        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
        [Required(ErrorMessage = "Daily start time is required")]
        [Display(Name = "Daily Start Time")]
        [DataType(DataType.Time)]
        public DateTime DailyStartTime { get; set; }
        
        [Required(ErrorMessage = "Daily end time is required")]
        [Display(Name = "Daily End Time")]
        [DataType(DataType.Time)]
        public DateTime DailyEndTime { get; set; }

        public CreateTimeSlotViewModel()
        {
            var today = DateTime.Now.Date;
            
            StartDate = today;
            EndDate = today.AddDays(7);
            
            DailyStartTime = new DateTime(2000, 1, 1, 9, 0, 0);
            DailyEndTime = new DateTime(2000, 1, 1, 17, 0, 0);
        }
    }
} 