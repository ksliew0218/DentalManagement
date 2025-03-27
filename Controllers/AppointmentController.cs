using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using DentalManagement.Models;
using DentalManagement.ViewModels;

namespace DentalManagement.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Appointment
        public async Task<IActionResult> Index()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.TreatmentType)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        // GET: Appointment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.TreatmentType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // GET: Appointment/Calendar
        public async Task<IActionResult> Calendar(int? doctorId)
        {
            ViewBag.DoctorId = doctorId;
            
            var doctors = await _context.Doctors
                .Where(d => !d.IsDeleted && d.Status == StatusType.Active)
                .OrderBy(d => d.LastName)
                .ToListAsync();
                
            ViewBag.Doctors = doctors;
            
            // If no doctor selected, use the first active doctor by default
            if (!doctorId.HasValue && doctors.Any())
            {
                ViewBag.DoctorId = doctors.First().Id;
            }
            
            return View();
        }
        
        // Endpoint to get appointments for the scheduler
        [HttpGet]
        public async Task<IActionResult> GetAppointments(int doctorId, DateTime start, DateTime end)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.TreatmentType)
                .Where(a => a.DoctorId == doctorId && 
                           a.AppointmentDate >= start.Date && 
                           a.AppointmentDate <= end.Date)
                .ToListAsync();
                
            var result = appointments.Select(a => new
            {
                id = a.Id,
                title = $"{a.Patient.FirstName} {a.Patient.LastName} - {a.TreatmentType.Name}",
                start = a.AppointmentDateTime,
                end = a.AppointmentDateTime.AddMinutes(a.TreatmentType.Duration),
                status = a.Status,
                patientId = a.PatientId,
                patientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                treatmentId = a.TreatmentTypeId,
                treatmentName = a.TreatmentType.Name,
                notes = a.Notes
            });
            
            return Json(result);
        }
        
        // Dashboard view
        public async Task<IActionResult> Dashboard()
        {
            // Get counts for dashboard
            var appointmentCounts = await _context.Appointments
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
                
            var todayAppointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.TreatmentType)
                .Where(a => a.AppointmentDate.Date == DateTime.Today)
                .OrderBy(a => a.AppointmentTime)
                .ToListAsync();
                
            var dashboardViewModel = new DashboardViewModel
            {
                TotalScheduledAppointments = appointmentCounts.Where(a => a.Status == "Scheduled").Sum(a => a.Count),
                TotalCompletedAppointments = appointmentCounts.Where(a => a.Status == "Completed").Sum(a => a.Count),
                TotalCancelledAppointments = appointmentCounts.Where(a => a.Status == "Cancelled").Sum(a => a.Count),
                TodayAppointments = todayAppointments
            };
            
            return View(dashboardViewModel);
        }
        
        // CRUD operations for appointments
        
        // GET: Appointment/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Doctors = await _context.Doctors
                .Where(d => !d.IsDeleted && d.Status == StatusType.Active)
                .OrderBy(d => d.LastName)
                .ToListAsync();
                
            ViewBag.Patients = await _context.Patients
                .OrderBy(p => p.LastName)
                .ToListAsync();
                
            ViewBag.TreatmentTypes = await _context.TreatmentTypes
                .OrderBy(t => t.Name)
                .ToListAsync();
                
            return View();
        }
        
        // POST: Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,DoctorId,TreatmentTypeId,AppointmentDate,AppointmentTime,Notes")] Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                // Check if the slot is available
                var appointmentEnd = appointment.AppointmentDateTime.AddMinutes(
                    _context.TreatmentTypes.First(t => t.Id == appointment.TreatmentTypeId).Duration);
                    
                var conflictingAppointment = await _context.Appointments
                    .Where(a => a.DoctorId == appointment.DoctorId &&
                               a.AppointmentDate.Date == appointment.AppointmentDate.Date &&
                               ((a.AppointmentTime <= appointment.AppointmentTime && 
                                 a.AppointmentTime.Add(TimeSpan.FromMinutes(_context.TreatmentTypes
                                     .First(t => t.Id == a.TreatmentTypeId).Duration)) > appointment.AppointmentTime) ||
                                (a.AppointmentTime >= appointment.AppointmentTime && 
                                 a.AppointmentTime < appointment.AppointmentTime.Add(TimeSpan.FromMinutes(_context.TreatmentTypes
                                     .First(t => t.Id == appointment.TreatmentTypeId).Duration))))
                               )
                    .FirstOrDefaultAsync();
                    
                if (conflictingAppointment != null)
                {
                    ModelState.AddModelError("", "The selected time slot conflicts with an existing appointment.");
                    
                    ViewBag.Doctors = await _context.Doctors
                        .Where(d => !d.IsDeleted && d.Status == StatusType.Active)
                        .OrderBy(d => d.LastName)
                        .ToListAsync();
                        
                    ViewBag.Patients = await _context.Patients
                        .OrderBy(p => p.LastName)
                        .ToListAsync();
                        
                    ViewBag.TreatmentTypes = await _context.TreatmentTypes
                        .OrderBy(t => t.Name)
                        .ToListAsync();
                        
                    return View(appointment);
                }
                
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            ViewBag.Doctors = await _context.Doctors
                .Where(d => !d.IsDeleted && d.Status == StatusType.Active)
                .OrderBy(d => d.LastName)
                .ToListAsync();
                
            ViewBag.Patients = await _context.Patients
                .OrderBy(p => p.LastName)
                .ToListAsync();
                
            ViewBag.TreatmentTypes = await _context.TreatmentTypes
                .OrderBy(t => t.Name)
                .ToListAsync();
                
            return View(appointment);
        }
    }
} 