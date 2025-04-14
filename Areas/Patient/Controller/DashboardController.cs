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
        var patientType = "Regular Patient"; 
        int patientId = 0;

        if (user != null)
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserID == user.Id);
                
            if (patient != null)
            {
                fullName = $"{patient.FirstName} {patient.LastName}";
                gender = patient.Gender;
                patientId = patient.Id;
                
                age = DateTime.Today.Year - patient.DateOfBirth.Year;
                if (patient.DateOfBirth.Date > DateTime.Today.AddYears(-age))
                {
                    age--;
                }
            }
        }

        var appointments = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.TreatmentType)
            .Where(a => a.PatientId == patientId && 
                       a.Status != "Cancelled" && 
                       a.Status != "Completed")
            .ToListAsync();

        var upcomingAppointment = appointments
            .Where(a => a.AppointmentDate > DateTime.Today || 
                       (a.AppointmentDate == DateTime.Today && a.AppointmentTime > DateTime.Now.TimeOfDay))
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.AppointmentTime)
            .Select(a => new
            {
                Id = a.Id,
                AppointmentDate = a.AppointmentDate.ToString("MMMM d, yyyy"),
                AppointmentTime = string.Format("{0:hh\\:mm tt}", DateTime.Today.Add(a.AppointmentTime)),
                DoctorName = $"Dr. {a.Doctor.FirstName} {a.Doctor.LastName}",
                AppointmentType = a.TreatmentType.Name,
                CanBeCancelled = a.CanBeCancelled(),
                Status = a.Status
            })
            .FirstOrDefault();

        if (upcomingAppointment == null)
        {
            upcomingAppointment = new
            {
                Id = 0,
                AppointmentDate = "No upcoming appointment",
                AppointmentTime = "",
                DoctorName = "",
                AppointmentType = "",
                CanBeCancelled = false,
                Status = ""
            };
        }

        var lastCompletedAppointment = await _context.Appointments
            .Where(a => a.PatientId == patientId && a.Status == "Completed")
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.AppointmentTime)
            .Select(a => new
            {
                CompletedDate = a.AppointmentDate.ToString("d MMMM yyyy")
            })
            .FirstOrDefaultAsync();

        ViewData["LastVisitDate"] = lastCompletedAppointment != null 
            ? lastCompletedAppointment.CompletedDate 
            : "No previous visits";

        ViewData["FullName"] = fullName;
        ViewData["Gender"] = gender;
        ViewData["Age"] = age;
        ViewData["PatientType"] = patientType;
        ViewData["UpcomingAppointment"] = upcomingAppointment;
        
        var latestTreatments = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.TreatmentType)
            .Where(a => a.PatientId == patientId && a.Status == "Completed")
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.AppointmentTime)
            .Take(3)
            .Select(a => new 
            { 
                TreatmentDate = a.AppointmentDate.ToString("MMMM d, yyyy"),
                TreatmentType = a.TreatmentType.Name,
                DoctorName = $"Dr. {a.Doctor.FirstName} {a.Doctor.LastName}"
            })
            .ToListAsync();
            
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
            var treatmentDetails = await _context.TreatmentTypes
                .Where(t => t.Name == treatmentName && t.IsActive && !t.IsDeleted)
                .Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    description = t.Description ?? "No description available.",
                    price = t.Price,
                    duration = t.Duration,
                    imageUrl = t.ImageUrl, 
                    doctors = t.DoctorTreatments
                        .Where(dt => dt.IsActive && !dt.IsDeleted)
                        .Select(dt => new 
                        { 
                            id = dt.Doctor.Id,
                            name = dt.Doctor.FirstName + " " + dt.Doctor.LastName, 
                            specialty = dt.Doctor.Specialty ?? "General Dentistry",
                            profilePictureUrl = dt.Doctor.ProfilePictureUrl ?? ""
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