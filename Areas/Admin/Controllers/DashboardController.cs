using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DentalManagement.Models;
using System;
using DentalManagement.Authorization;

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
                // Get counts for dashboard
                ViewBag.DoctorCount = _context.Doctors.Count(d => !d.IsDeleted);
                ViewBag.PatientCount = _context.Patients.Count();
                ViewBag.TreatmentCount = _context.TreatmentTypes.Count();
                
                return View();
            }
            catch (Exception ex)
            {
                // Handle any errors gracefully
                ViewBag.ErrorMessage = "Error loading dashboard data: " + ex.Message;
                ViewBag.DoctorCount = 0;
                ViewBag.PatientCount = 0;
                ViewBag.TreatmentCount = 0;
                return View();
            }
        }
    }
} 