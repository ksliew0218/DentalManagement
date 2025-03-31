using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DentalManagement.Models;
using DentalManagement.Areas.Doctor.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DentalManagement.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public AppointmentsController(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Doctor/Appointments
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

                // Get all appointments for the doctor
                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.TreatmentType)
                    .Where(a => a.DoctorId == doctor.Id)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .ToListAsync();

                return View(appointments);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading appointments: " + ex.Message;
                return View(new List<Appointment>());
            }
        }

        // GET: Doctor/Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.TreatmentType)
                .Include(a => a.TimeSlots)
                .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctor.Id);

            if (appointment == null)
            {
                return NotFound();
            }

            // Set doctor name in ViewData for the layout
            ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";

            return View(appointment);
        }

        // GET: Doctor/Appointments/Calendar
        public async Task<IActionResult> Calendar()
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

            // Get all appointments for the doctor
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.TreatmentType)
                .Where(a => a.DoctorId == doctor.Id)
                .ToListAsync();

            // Convert appointments to calendar events with enhanced titles
            var events = appointments.Select(a => new
            {
                id = a.Id,
                title = $"{a.AppointmentTime.Hours:D2}:{a.AppointmentTime.Minutes:D2} - {a.Patient?.FirstName} {a.Patient?.LastName}",
                start = a.AppointmentDate.Date.Add(a.AppointmentTime).ToString("o"),
                end = a.AppointmentDate.Date.Add(a.AppointmentTime).AddMinutes(a.Duration).ToString("o"),
                color = GetStatusColor(a.Status),
                status = a.Status,
                treatment = a.TreatmentType?.Name,
                url = Url.Action("Details", "Appointments", new { id = a.Id, area = "Doctor" })
            }).ToList();

            ViewBag.CalendarEvents = System.Text.Json.JsonSerializer.Serialize(events);

            return View();
        }

        // Helper method to get color based on appointment status (simplified to 4 statuses)
        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Scheduled" => "#4e73df", // Blue
                "Completed" => "#1cc88a", // Green
                "Cancelled" => "#e74a3b", // Red
                "No-Show" => "#f6c23e", // Yellow
                _ => "#4e73df", // Default blue
            };
        }

        // GET: Doctor/Appointments/UpdateStatus/5?status=Completed
        public async Task<IActionResult> UpdateStatus(int? id, string status)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Validate status
            if (string.IsNullOrEmpty(status) || 
                (status != "Scheduled" && status != "Completed" && status != "Cancelled" && status != "No-Show"))
            {
                TempData["ErrorMessage"] = "Invalid status value provided.";
                return RedirectToAction(nameof(Details), new { id });
            }

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

            // Find the appointment
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctor.Id);

            if (appointment == null)
            {
                return NotFound();
            }

            try
            {
                // Update the appointment status
                appointment.Status = status;
                appointment.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Appointment status has been updated to {status}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating appointment status: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
} 