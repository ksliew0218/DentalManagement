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
                var model = new AdminDashboardViewModel
                {
                    DoctorCount = _context.Doctors.Count(d => !d.IsDeleted),
                    PatientCount = _context.Patients.Count(),
                    TreatmentTypeCount = _context.TreatmentTypes.Where(t => !t.IsDeleted).Count(),
                    AppointmentCount = _context.Appointments.Count(),
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading dashboard data: " + ex.Message;
                
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