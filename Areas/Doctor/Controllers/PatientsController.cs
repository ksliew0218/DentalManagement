using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using DentalManagement.Models;

namespace DentalManagement.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public PatientsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Doctor/Patients
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
                    .FirstOrDefaultAsync(d => d.User.Id == user.Id);
                
                if (doctor == null)
                {
                    // The current user is not a doctor
                    return RedirectToAction("AccessDenied", "Home", new { area = "" });
                }

                // Set doctor name in ViewData for the layout
                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";

                // Get all appointments for this doctor with their associated patients
                var patientsQuery = await _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id)
                    .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                    .Select(a => a.Patient)
                    .Distinct()
                    .Where(p => !p.IsDeleted)
                    .ToListAsync();

                return View(patientsQuery);
            }
            catch (Exception ex)
            {
                ViewData["DoctorName"] = "Doctor";
                ViewBag.ErrorMessage = "Error loading patients: " + ex.Message;
                return View(new List<DentalManagement.Models.Patient>());
            }
        }

        // GET: Doctor/Patients/Details/5
        public async Task<IActionResult> Details(int id)
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
                    .FirstOrDefaultAsync(d => d.User.Id == user.Id);
                
                if (doctor == null)
                {
                    // The current user is not a doctor
                    return RedirectToAction("AccessDenied", "Home", new { area = "" });
                }

                // Set doctor name in ViewData for the layout
                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";

                // Check if the patient exists and has had appointments with this doctor
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

                if (patient == null)
                {
                    return NotFound();
                }

                // Check if this doctor has treated this patient
                var hasAppointmentWithDoctor = await _context.Appointments
                    .AnyAsync(a => a.DoctorId == doctor.Id && a.PatientId == id);

                if (!hasAppointmentWithDoctor)
                {
                    TempData["ErrorMessage"] = "You don't have access to this patient's information";
                    return RedirectToAction(nameof(Index));
                }

                // Get patient's appointments with this doctor
                var appointments = await _context.Appointments
                    .Include(a => a.TreatmentType)
                    .Include(a => a.TreatmentReports)
                    .Where(a => a.DoctorId == doctor.Id && a.PatientId == id)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();

                ViewBag.Appointments = appointments;

                return View(patient);
            }
            catch (Exception ex)
            {
                ViewData["DoctorName"] = "Doctor";
                ViewBag.ErrorMessage = "Error loading patient details: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 