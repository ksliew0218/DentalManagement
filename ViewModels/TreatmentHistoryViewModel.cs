using System;

namespace DentalManagement.ViewModels
{
    public class TreatmentHistoryViewModel
    {
        public int AppointmentId { get; set; }
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string? Notes { get; set; }
        
        // Formatted properties for display
        public string FormattedDate => AppointmentDate.ToString("MMM dd, yyyy");
        public string FormattedTime
        {
            get
            {
                int hour12 = AppointmentTime.Hours % 12;
                if (hour12 == 0) hour12 = 12;
                string period = AppointmentTime.Hours >= 12 ? "PM" : "AM";
                return $"{hour12}:{AppointmentTime.Minutes:D2} {period}";
            }
        }
        
        public string Status => "Completed"; // Since we're only fetching completed appointments
    }
} 