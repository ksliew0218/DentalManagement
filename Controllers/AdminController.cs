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
using Microsoft.EntityFrameworkCore;

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
                var model = new AdminDashboardViewModel
                {
                    DoctorCount = _context.Doctors.Count(),
                    PatientCount = _context.Patients.Count(),
                    TreatmentTypeCount = _context.TreatmentTypes.Where(t => !t.IsDeleted).Count(),
                    AppointmentCount = 0,
                    TotalRevenue = 0 
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Dashboard action");
                
                ViewBag.ErrorMessage = "Error loading dashboard: " + ex.Message;
                return View("Error");
            }
        }
        
        public async Task<IActionResult> Patients()
        {
            try
            {
                var patients = await _context.Patients
                    .Include(p => p.User)
                    .Where(p => !p.IsDeleted)
                    .ToListAsync();
                
                return View(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading patients");
                ViewBag.ErrorMessage = "Error loading patients: " + ex.Message;
                return View("Error");
            }
        }
        
        public async Task<IActionResult> PatientDetails(int id)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

                if (patient == null)
                {
                    return NotFound();
                }

                var appointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.TreatmentType)
                    .Include(a => a.TreatmentReports)
                    .Include(a => a.Payments)
                    .Where(a => a.PatientId == id)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();

                ViewBag.Appointments = appointments;
                
                int age = DateTime.Today.Year - patient.DateOfBirth.Year;
                if (patient.DateOfBirth.Date > DateTime.Today.AddYears(-age))
                {
                    age--;
                }
                ViewBag.Age = age;

                return View(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading patient details for ID {PatientId}", id);
                ViewBag.ErrorMessage = "Error loading patient details: " + ex.Message;
                return RedirectToAction(nameof(Patients));
            }
        }

        public async Task<IActionResult> PatientTreatmentReport(int id)
        {
            try
            {
                var treatmentReport = await _context.TreatmentReports
                    .Include(tr => tr.Doctor)
                    .Include(tr => tr.Patient)
                    .Include(tr => tr.Appointment)
                    .ThenInclude(a => a.TreatmentType)
                    .FirstOrDefaultAsync(tr => tr.Id == id);

                if (treatmentReport == null)
                {
                    return NotFound();
                }

                var payments = await _context.Payments
                    .Where(p => p.AppointmentId == treatmentReport.AppointmentId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                ViewBag.Payments = payments;

                return View(treatmentReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading treatment report for ID {ReportId}", id);
                ViewBag.ErrorMessage = "Error loading treatment report: " + ex.Message;
                return RedirectToAction(nameof(Patients));
            }
        }
        public async Task<IActionResult> PatientAppointmentDetails(int id)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .Include(a => a.TreatmentType)
                    .Include(a => a.TreatmentReports)
                    .Include(a => a.Payments)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (appointment == null)
                {
                    return NotFound();
                }

                decimal amountPaid = 0;
                if (appointment.Payments != null && appointment.Payments.Any())
                {
                    amountPaid = appointment.Payments
                        .Where(p => p.Status == "succeeded")
                        .Sum(p => p.Amount);
                }

                ViewBag.AmountPaid = amountPaid;
                ViewBag.RemainingAmount = appointment.TotalAmount - amountPaid;

                return View(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading appointment details for ID {AppointmentId}", id);
                ViewBag.ErrorMessage = "Error loading appointment details: " + ex.Message;
                return RedirectToAction(nameof(Patients));
            }
        }
        
        public async Task<IActionResult> Appointments()
        {
            try
            {
                var appointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .Include(a => a.TreatmentType)
                    .Include(a => a.Payments)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .ToListAsync();

                return View(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading appointments");
                ViewBag.ErrorMessage = "Error loading appointments: " + ex.Message;
                return View("Error");
            }
        }
    }
} 