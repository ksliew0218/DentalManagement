using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DentalManagement.Models;
using DentalManagement.Areas.Doctor.Models;
using DentalManagement.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DentalManagement.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get the current logged-in user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                // Get the doctor profile for the current user
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .Include(d => d.DoctorTreatments)
                    .ThenInclude(dt => dt.TreatmentType)
                    .FirstOrDefaultAsync(d => d.User.Id == user.Id);

                if (doctor == null)
                {
                    // The current user is not a doctor
                    return RedirectToAction("AccessDenied", "Home", new { area = "" });
                }

                // Set doctor name in ViewData for the layout
                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";

                // Create the dashboard view model
                var model = new DoctorDashboardViewModel
                {
                    CurrentDoctor = doctor,
                    TreatmentCount = doctor.DoctorTreatments?.Count ?? 0,
                    TimeSlotCount = await _context.TimeSlots.CountAsync(s => s.DoctorId == doctor.Id),
                    AppointmentCount = 0, // Update once appointments are implemented
                    PatientCount = 0, // Update once patients for doctors are implemented
                    UpcomingAppointments = 0, // Update once appointments are implemented
                    
                    // Get recent time slots (next 5 days)
                    RecentTimeSlots = await _context.TimeSlots
                        .Where(s => s.DoctorId == doctor.Id)
                        .Where(s => s.StartTime >= DateTime.UtcNow)
                        .OrderBy(s => s.StartTime)
                        .Take(5)
                        .ToListAsync()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                // Handle any errors gracefully
                ViewData["DoctorName"] = "Doctor";
                ViewBag.ErrorMessage = "Error loading dashboard data: " + ex.Message;
                
                // Return a model with default values
                return View(new DoctorDashboardViewModel());
            }
        }
    }
} 