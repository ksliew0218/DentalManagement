using Microsoft.AspNetCore.Mvc;

namespace DentalManagement.Areas.Patient.Controllers
{
    [Area("Patient")]
    public class NotificationsController : Controller
    {
        public IActionResult Index()
        {
            return PartialView("_Notifications");
        }
    }
}
