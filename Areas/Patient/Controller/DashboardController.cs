using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DentalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using DentalManagement.Authorization;
namespace DentalManagement.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize]
    [PatientOnly]
    public class DashboardController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardController(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var fullName = "Guest";

            // Hardcoded user name, can be dynamic based on actual user later
            if (user != null)
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
                if (patient != null)
                {
                    fullName = $"{patient.FirstName} {patient.LastName}";
                }
            }

            // Hardcoded counts for dashboard data
            var patientCount = 10;  // Hardcoded value
            var surgeryCount = 5;   // Hardcoded value
            var dischargeCount = 3; // Hardcoded value

            var upcomingAppointment = new
            {
                AppointmentDate = "March 25, 2024",
                AppointmentTime = "10:30 AM",
                DoctorName = "Dr. Sarah Wilson",
                AppointmentType = "Regular Checkup"
            };

            var latestTreatments = new[]
            {
                new { TreatmentDate = "March 15, 2024", TreatmentType = "Dental Cleaning", DoctorName = "Dr. John Smith" },
                new { TreatmentDate = "February 28, 2024", TreatmentType = "Cavity Filling", DoctorName = "Dr. Sarah Wilson" },
                new { TreatmentDate = "January 15, 2024", TreatmentType = "X-Ray Examination", DoctorName = "Dr. Mike Johnson" }
            };

            var availableTreatments = new[]
            {
                new { TreatmentName = "Teeth Cleaning", Description = "Professional cleaning to remove plaque and tartar." },
                new { TreatmentName = "Teeth Whitening", Description = "Professional whitening for a brighter smile." },
                new { TreatmentName = "Root Canal", Description = "Treatment for infected or damaged tooth pulp." },
                new { TreatmentName = "Dental Crown", Description = "Custom-made caps to cover damaged teeth." }
            };

            var ongoingTreatment = new
            {
                TreatmentType = "Braces",
                StartDate = "February 1, 2024",
                CompletionDate = "March 15, 2024",
                ProgressPercentage = 60,
                NextAppointment = "March 10, 2024"
            };

            // Pass hardcoded data to ViewData
            ViewData["FullName"] = fullName;
            ViewData["PatientCount"] = patientCount;
            ViewData["SurgeryCount"] = surgeryCount;
            ViewData["DischargeCount"] = dischargeCount;
            ViewData["UpcomingAppointment"] = upcomingAppointment; // Passing upcoming appointment data
            ViewData["LatestTreatments"] = latestTreatments; // Passing latest treatments data
            ViewData["AvailableTreatments"] = availableTreatments; // Passing available treatments data
            ViewData["OngoingTreatment"] = ongoingTreatment;

            return PartialView("_Dashboard"); // Returning PartialView
        }
    }
}
