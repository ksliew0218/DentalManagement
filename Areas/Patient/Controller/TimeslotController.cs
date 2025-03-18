using Microsoft.AspNetCore.Mvc;

namespace DentalManagement.Areas.Patient.Controllers
{
    [Area("Patient")]
    public class TimeSlots : Controller
    {
        public IActionResult Index()
        {
            return PartialView("_Timeslots");
        }
    }
}
