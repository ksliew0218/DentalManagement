using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DentalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DentalManagement.Authorization;

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
                // Get counts for dashboard
                ViewBag.DoctorCount = _context.Doctors.Count();
                ViewBag.PatientCount = _context.Patients.Count();
                ViewBag.TreatmentCount = _context.TreatmentTypes.Count();
                
                return View();
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