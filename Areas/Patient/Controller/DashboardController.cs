using Microsoft.AspNetCore.Mvc;
using DentalManagement.Authorization;
using DentalManagement.Areas.Patient.Models;

namespace DentalManagement.Areas.Patient.Controllers
{
    [Area("Patient")]
    [PatientOnly]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            var model = new PatientDashboardViewModel
            {
                AssignedDoctors = 2,
                UpcomingAppointments = 3,
                MedicalRecords = 5,
                ActivePrescriptions = 2
            };

            return View(model);
        }
    }
}
