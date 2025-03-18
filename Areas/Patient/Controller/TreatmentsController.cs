using Microsoft.AspNetCore.Mvc;

namespace DentalManagement.Areas.Patient.Controllers
{
    [Area("Patient")]
    public class Treatments : Controller
    {
        public IActionResult Index()
        {
            return PartialView("_Treatments");
        }
    }
}
