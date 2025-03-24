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
            // Initialize with UTC values
            var now = DateTime.UtcNow;
            StartDate = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
            EndDate = DateTime.SpecifyKind(now.Date.AddDays(7), DateTimeKind.Utc);
            
            // Set 9:00 AM
            var startTime = now.Date.AddHours(9);
            DailyStartTime = DateTime.SpecifyKind(startTime, DateTimeKind.Utc);
            
            // Set 5:00 PM
            var endTime = now.Date.AddHours(17);
            DailyEndTime = DateTime.SpecifyKind(endTime, DateTimeKind.Utc);
        }
    }
} 