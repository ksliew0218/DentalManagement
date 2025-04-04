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
using DentalManagement.Services;


namespace DentalManagement.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;



        public AppointmentsController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IEmailService emailService)

        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;

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
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Include(a => a.TreatmentType)
                .Include(a => a.TimeSlots)
                .Include(a => a.TreatmentReports)
                .Include(a => a.Payments)
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
                "Confirmed" => "#4796ff", // Lighter Blue
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
                (status != "Confirmed" && status != "Completed" && status != "Cancelled" && status != "No-Show"))
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

            // Find the appointment with all necessary data for email
            var appointment = await _context.Appointments
                .Include(a => a.TreatmentReports)
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                .Include(a => a.TreatmentType)
                .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctor.Id);

            if (appointment == null)
            {
                return NotFound();
            }

            try
            {
                // Check if status is already the same
                if (appointment.Status == status)
                {
                    TempData["InfoMessage"] = $"Appointment is already marked as {status}.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                // Check if appointment is already completed - cannot be changed
                if (appointment.Status == "Completed" && status != "Completed")
                {
                    TempData["ErrorMessage"] = "Cannot modify an appointment that has been marked as Completed.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Calculate if appointment is in the past
                DateTime appointmentDateTime = appointment.AppointmentDate.Date + appointment.AppointmentTime;
                bool isPastAppointment = appointmentDateTime < DateTime.Now;
                
                // For Completed status, we'll allow it regardless of appointment time
                // For No-Show, only allow if it's a past appointment
                if (status == "No-Show" && !isPastAppointment)
                {
                    TempData["ErrorMessage"] = "Cannot mark a future appointment as No-Show.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                // Cancel can be done anytime for future appointments or same-day appointments
                if (status == "Cancelled" && appointmentDateTime.Date < DateTime.Today)
                {
                    TempData["ErrorMessage"] = "Cannot cancel a past appointment. Please use 'No-Show' instead.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Update the appointment status
                appointment.Status = status;
                appointment.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Appointment status has been updated to {status}.";
                
                // If completed, prompt for treatment report and send completion email
                if (status == "Completed")
                {
                    TempData["ShowTreatmentReportModal"] = true;
                    
                    // Calculate remaining balance - using the TotalAmount and DepositAmount
                    decimal remainingBalance = appointment.TotalAmount - appointment.DepositAmount;
                    
                    // Create appointment details view model
                    var appointmentDetails = new DentalManagement.Areas.Patient.Models.AppointmentDetailViewModel
                    {
                        Id = appointment.Id,
                        TreatmentName = appointment.TreatmentType.Name,
                        DoctorName = $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}",
                        DoctorSpecialization = appointment.Doctor.Specialty,
                        AppointmentDate = appointment.AppointmentDate,
                        AppointmentTime = appointment.AppointmentTime,
                        Status = status,
                        CreatedOn = appointment.CreatedAt,
                        TreatmentCost = appointment.TotalAmount,
                        TreatmentDuration = appointment.Duration
                    };
                    
                    // Create in-app notification for completed appointment
                    await CreateAppointmentCompletedNotification(appointment);
                    
                    // Send the completion email
                    await _emailService.SendAppointmentCompletedEmailAsync(
                        appointment.Patient.User.Email,
                        $"{appointment.Patient.FirstName} {appointment.Patient.LastName}",
                        appointmentDetails,
                        remainingBalance
                    );
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating appointment status: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Helper method to create a notification for completed appointments
        private async Task CreateAppointmentCompletedNotification(Appointment appointment)
        {
            try 
            {
                var user = appointment.Patient.User;
                var treatment = appointment.TreatmentType;
                var doctor = appointment.Doctor;

                // Format appointment date and time
                string formattedDate = appointment.AppointmentDate.ToString("MMMM d, yyyy");
                bool isPM = appointment.AppointmentTime.Hours >= 12;
                int hour12 = appointment.AppointmentTime.Hours % 12;
                if (hour12 == 0) hour12 = 12;
                string formattedTime = $"{hour12}:{appointment.AppointmentTime.Minutes:D2} {(isPM ? "PM" : "AM")}";

                // Calculate remaining balance
                decimal remainingBalance = appointment.TotalAmount - appointment.DepositAmount;
                string paymentMessage = remainingBalance > 0 
                    ? $" You have a remaining balance of RM{remainingBalance:0.00} to pay."
                    : " Your payment is complete.";

                // Create in-app notification
                var notification = new UserNotification
                {
                    UserId = user.Id,
                    NotificationType = "Appointment_Completed",
                    Title = "Appointment Completed",
                    Message = $"Your {treatment.Name} appointment with Dr. {doctor.FirstName} {doctor.LastName} on {formattedDate} at {formattedTime} has been completed.{paymentMessage}",
                    RelatedEntityId = appointment.Id,
                    ActionController = "Appointments",
                    ActionName = "Details",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserNotifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Silently handle the error - no logging
                // This prevents errors from stopping the status update process
            }
        }
        // POST: Doctor/Appointments/AddTreatmentReport/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTreatmentReport(int id, string treatmentNotes, string dentalChart)
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

            // Find the appointment
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctor.Id);

            if (appointment == null)
            {
                return NotFound();
            }

            try
            {
                // Create a new TreatmentReport if it doesn't exist
                var treatmentReport = await _context.TreatmentReports
                    .FirstOrDefaultAsync(tr => tr.AppointmentId == id);
                
                if (treatmentReport == null)
                {
                    treatmentReport = new TreatmentReport
                    {
                        AppointmentId = id,
                        DoctorId = doctor.Id,
                        PatientId = appointment.PatientId,
                        TreatmentDate = DateTime.Now,
                        Notes = treatmentNotes,
                        DentalChart = dentalChart,
                        CreatedAt = DateTime.Now
                    };
                    
                    _context.TreatmentReports.Add(treatmentReport);
                }
                else
                {
                    // Update existing report
                    treatmentReport.Notes = treatmentNotes;
                    treatmentReport.DentalChart = dentalChart;
                    treatmentReport.UpdatedAt = DateTime.Now;
                }
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Treatment report has been saved successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving treatment report: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTreatmentReport(int id, int reportId, string treatmentNotes, string dentalChart)
        {
            try
            {
                // Validate parameters
                if (id <= 0 || reportId <= 0 || string.IsNullOrEmpty(treatmentNotes))
                {
                    TempData["ErrorMessage"] = "Invalid parameters provided.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                // Get the current user and check if they have a doctor profile
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
                
                // Get the appointment and treatment report
                var appointment = await _context.Appointments
                    .Include(a => a.TreatmentReports)
                    .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctor.Id);
                    
                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Appointment not found.";
                    return RedirectToAction(nameof(Index));
                }
                
                var report = appointment.TreatmentReports?.FirstOrDefault(tr => tr.Id == reportId);
                if (report == null)
                {
                    TempData["ErrorMessage"] = "Treatment report not found.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                // Update the treatment report
                report.Notes = treatmentNotes;
                report.DentalChart = dentalChart;
                report.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Treatment report updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating treatment report: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
} 