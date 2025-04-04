using System;

namespace DentalManagement.Areas.Doctor.Models
{
    public class DoctorAppointmentNotificationViewModel
    {
        // Appointment Details
        public int AppointmentId { get; set; }
        public string TreatmentName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public int TreatmentDuration { get; set; }

        // Doctor Details
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string DoctorEmail { get; set; }

        // Patient Details
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string PatientEmail { get; set; }
        public string PatientPhoneNumber { get; set; }

        // Formatting Methods
        public string FormattedAppointmentDate => AppointmentDate.ToString("MMMM d, yyyy");
        public string FormattedAppointmentTime 
        {
            get 
            {
                bool isPM = AppointmentTime.Hours >= 12;
                int hour12 = AppointmentTime.Hours % 12;
                if (hour12 == 0) hour12 = 12;
                return $"{hour12}:{AppointmentTime.Minutes:D2} {(isPM ? "PM" : "AM")}";
            }
        }
    }
}