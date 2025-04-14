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

        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.User.Id == user.Id);
                
                if (doctor == null)
                {
                    return RedirectToAction("AccessDenied", "Home", new { area = "" });
                }

                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;

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

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.User.Id == user.Id);
                
                if (doctor == null)
                {
                    return RedirectToAction("AccessDenied", "Home", new { area = "" });
                }

                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;

                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

                if (patient == null)
                {
                    return NotFound();
                }

                var hasAppointmentWithDoctor = await _context.Appointments
                    .AnyAsync(a => a.DoctorId == doctor.Id && a.PatientId == id);

                if (!hasAppointmentWithDoctor)
                {
                    TempData["ErrorMessage"] = "You don't have access to this patient's information";
                    return RedirectToAction(nameof(Index));
                }

                var appointments = await _context.Appointments
                    .Include(a => a.TreatmentType)
                    .Include(a => a.TreatmentReports)
                    .Include(a => a.Payments)
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