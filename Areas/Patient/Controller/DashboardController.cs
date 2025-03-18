using Microsoft.AspNetCore.Mvc;

namespace DentalManagement.Areas.Patient.Controllers
{
    [Area("Patient")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            // Example data, you can replace these with real data from a database or API
            var patientCount = 9; // Example count of patients
            var surgeryCount = 3; // Example count of surgeries
            var dischargeCount = 2; // Example count of discharges

            // Passing data to the view via ViewData
            ViewData["PatientCount"] = patientCount;
            ViewData["SurgeryCount"] = surgeryCount;
            ViewData["DischargeCount"] = dischargeCount;

            return PartialView("_Dashboard");
        }
    }
}
