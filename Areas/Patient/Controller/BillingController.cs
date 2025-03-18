using Microsoft.AspNetCore.Mvc;

namespace DentalManagement.Areas.Patient.Controllers
{
    [Area("Patient")]
    public class BillingController : Controller
    {
        public IActionResult Index()
        {
            return PartialView("_Billing");
        }
    }
}
