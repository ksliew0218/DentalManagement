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
        var gender = "";
        var age = 0;
        var patientType = "Regular Patient"; // Hardcoded for now

        // Get patient data from database
        if (user != null)
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserID == user.Id);
                
            if (patient != null)
            {
                fullName = $"{patient.FirstName} {patient.LastName}";
                gender = patient.Gender;
                
                // Calculate age from DateOfBirth
                age = DateTime.Today.Year - patient.DateOfBirth.Year;
                // Adjust age if birthday hasn't occurred yet this year
                if (patient.DateOfBirth.Date > DateTime.Today.AddYears(-age))
                {
                    age--;
                }
            }
        }

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

            var availableTreatments = await _context.TreatmentTypes
                .Where(t => t.IsActive && !t.IsDeleted)
                .OrderBy(t => t.Name)
                .Select(t => new 
                { 
                    TreatmentName = t.Name, 
                    Description = t.Description ?? "No description available.",
                    Price = t.Price,
                    Duration = t.Duration,
                    ImageUrl = t.ImageUrl
                })
                .ToListAsync();

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
        ViewData["Gender"] = gender;
        ViewData["Age"] = age;
        ViewData["PatientType"] = patientType;
        ViewData["UpcomingAppointment"] = upcomingAppointment;
        ViewData["LatestTreatments"] = latestTreatments;
        ViewData["AvailableTreatments"] = availableTreatments;
        ViewData["OngoingTreatment"] = ongoingTreatment;

            return PartialView("_Dashboard");
        }

        [HttpGet]
        [Route("/Patient/Treatments/GetTreatmentDetails")]
        public async Task<IActionResult> GetTreatmentDetails(string treatmentName)
        {
            try
            {
                // Get the treatment details
                var treatmentDetails = await _context.TreatmentTypes
                    .Where(t => t.Name == treatmentName && t.IsActive && !t.IsDeleted)
                    .Select(t => new
                    {
                        name = t.Name,
                        description = t.Description ?? "No description available.",
                        price = t.Price,
                        duration = t.Duration,
                        // Get doctors through the DoctorTreatment join table
                        doctors = t.DoctorTreatments
                            .Where(dt => dt.IsActive && !dt.IsDeleted)
                            .Select(dt => new 
                            { 
                                name = $"Dr. {dt.Doctor.FirstName} {dt.Doctor.LastName}",
                                specialty = dt.Doctor.Specialty ?? "General Dentistry"
                            })
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (treatmentDetails == null)
                {
                    return NotFound(new { message = "Treatment not found" });
                }

                return Json(treatmentDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching treatment details", error = ex.Message });
            }
        }
    }
}