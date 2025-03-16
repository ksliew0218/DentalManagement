namespace DentalManagement.Areas.Patient.Models
{
    public class PatientDashboardViewModel
    {
        public int AssignedDoctors { get; set; }
        public int UpcomingAppointments { get; set; }
        public int MedicalRecords { get; set; }
        public int ActivePrescriptions { get; set; }
    }
}
