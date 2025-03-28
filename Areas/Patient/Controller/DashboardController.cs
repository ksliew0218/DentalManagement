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
        int patientId = 0;

        // Get patient data from database
        if (user != null)
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserID == user.Id);
                
            if (patient != null)
            {
                fullName = $"{patient.FirstName} {patient.LastName}";
                gender = patient.Gender;
                patientId = patient.Id;
                
                // Calculate age from DateOfBirth
                age = DateTime.Today.Year - patient.DateOfBirth.Year;
                // Adjust age if birthday hasn't occurred yet this year
                if (patient.DateOfBirth.Date > DateTime.Today.AddYears(-age))
                {
                    age--;
                }
            }
        }

        // Get upcoming appointment from database
        var appointments = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.TreatmentType)
            .Where(a => a.PatientId == patientId && 
                       a.Status != "Cancelled" && 
                       a.Status != "Completed")
            .ToListAsync();

        // Process in memory to avoid timestamp comparison issues
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

        // If no upcoming appointment is found, set to null or a default object
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

        // Get the most recent completed appointment
        var lastCompletedAppointment = await _context.Appointments
            .Where(a => a.PatientId == patientId && a.Status == "Completed")
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.AppointmentTime)
            .Select(a => new
            {
                CompletedDate = a.AppointmentDate.ToString("d MMMM yyyy")
            })
            .FirstOrDefaultAsync();

        // Set default value if no completed appointments found
        ViewData["LastVisitDate"] = lastCompletedAppointment != null 
            ? lastCompletedAppointment.CompletedDate 
            : "No previous visits";

        ViewData["FullName"] = fullName;
        ViewData["Gender"] = gender;
        ViewData["Age"] = age;
        ViewData["PatientType"] = patientType;
        ViewData["UpcomingAppointment"] = upcomingAppointment;
        
        // Define latestTreatments that was referenced earlier
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
            
        // Get available treatments from database
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

        // For now, keep this as a hardcoded object until we implement treatment tracking
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
            // Get the treatment details
            var treatmentDetails = await _context.TreatmentTypes
                .Where(t => t.Name == treatmentName && t.IsActive && !t.IsDeleted)
                .Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    description = t.Description ?? "No description available.",
                    price = t.Price,
                    duration = t.Duration,
                    imageUrl = t.ImageUrl, // Include treatment image URL
                    // Get doctors through the DoctorTreatment join table
                    doctors = t.DoctorTreatments
                        .Where(dt => dt.IsActive && !dt.IsDeleted)
                        .Select(dt => new 
                        { 
                            id = dt.Doctor.Id,
                            name = dt.Doctor.FirstName + " " + dt.Doctor.LastName, // We'll add "Dr." prefix in the frontend
                            specialty = dt.Doctor.Specialty ?? "General Dentistry",
                            profilePictureUrl = dt.Doctor.ProfilePictureUrl ?? ""// Include doctor profile picture
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