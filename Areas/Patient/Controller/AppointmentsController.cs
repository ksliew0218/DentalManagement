using Microsoft.AspNetCore.Mvc;

namespace DentalManagement.Areas.Patient.Controllers
{
    [Area("Patient")]
    public class AppointmentsController : Controller
    {
        public IActionResult Index()
        {
            return PartialView("_MyAppointments");
        }
    }
}
