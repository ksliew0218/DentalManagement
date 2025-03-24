using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DentalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DentalManagement.Authorization;
using System.Linq;
using DentalManagement.ViewModels;

namespace DentalManagement.Controllers
{
    [Authorize]
    [AdminOnly]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public AdminController(
            ILogger<AdminController> logger,
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Create an AdminDashboardViewModel with proper data
                var model = new AdminDashboardViewModel
                {
                    DoctorCount = _context.Doctors.Count(),
                    PatientCount = _context.Patients.Count(),
                    TreatmentTypeCount = _context.TreatmentTypes.Where(t => !t.IsDeleted).Count(),
                    AppointmentCount = 0, // Update this if you implement appointments
                    TotalRevenue = 0 // Update this if you implement revenue tracking
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error in Dashboard action");
                
                // Return a simple view with error information
                ViewBag.ErrorMessage = "Error loading dashboard: " + ex.Message;
                return View("Error");
            }
        }
    }
} 