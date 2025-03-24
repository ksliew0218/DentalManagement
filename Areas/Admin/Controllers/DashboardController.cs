using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DentalManagement.Models;
using System;
using DentalManagement.Authorization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using DentalManagement.ViewModels;

namespace DentalManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [AdminOnly]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
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
                // Handle any errors gracefully
                ViewBag.ErrorMessage = "Error loading dashboard data: " + ex.Message;
                
                // Return a model with default values
                var defaultModel = new AdminDashboardViewModel
                {
                    DoctorCount = 0,
                    PatientCount = 0,
                    TreatmentTypeCount = 0,
                    AppointmentCount = 0,
                    TotalRevenue = 0
                };
                
                return View(defaultModel);
            }
        }
    }
} 