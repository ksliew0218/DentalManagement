using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DentalManagement.Models;
using DentalManagement.Areas.Doctor.Models;
using DentalManagement.Authorization;
using DentalManagement.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DentalManagement.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly LeaveManagementService _leaveService;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            LeaveManagementService leaveService)
        {
            _context = context;
            _userManager = userManager;
            _leaveService = leaveService;
        }

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
                    .Include(d => d.User)
                    .Include(d => d.DoctorTreatments)
                    .ThenInclude(dt => dt.TreatmentType)
                    .FirstOrDefaultAsync(d => d.User.Id == user.Id);

                if (doctor == null)
                {
                    // The current user is not a doctor
                    return RedirectToAction("AccessDenied", "Home", new { area = "" });
                }

                // Set doctor name in ViewData for the layout
                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                // Add doctor profile picture URL to ViewData
                ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;
                
                // Get leave balances for the current year
                var currentYear = DateTime.UtcNow.Year;
                var leaveBalances = await _leaveService.GetDoctorLeaveBalancesAsync(doctor.Id, currentYear);
                
                // Get pending leave requests
                var leaveRequests = await _leaveService.GetDoctorLeaveRequestsAsync(doctor.Id);
                var pendingLeaveRequests = leaveRequests.Where(r => r.Status == LeaveRequestStatus.Pending).ToList();
                var upcomingApprovedLeave = leaveRequests
                    .Where(r => r.Status == LeaveRequestStatus.Approved && r.StartDate > DateTime.UtcNow)
                    .OrderBy(r => r.StartDate)
                    .FirstOrDefault();

                // Get appointment counts and upcoming appointments
                var allAppointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id)
                    .ToListAsync();
                
                var upcomingAppointments = allAppointments
                    .Where(a => a.AppointmentDateTime > DateTime.UtcNow && 
                                a.Status != "Cancelled" && 
                                a.Status != "Completed")
                    .Count();
                
                // Get list of patients seen by this doctor
                var patientIds = allAppointments.Select(a => a.PatientId).Distinct().ToList();
                var patientCount = patientIds.Count;

                // Create the dashboard view model
                var model = new DoctorDashboardViewModel
                {
                    CurrentDoctor = doctor,
                    TreatmentCount = doctor.DoctorTreatments?.Count ?? 0,
                    TimeSlotCount = await _context.TimeSlots.CountAsync(s => s.DoctorId == doctor.Id),
                    AppointmentCount = allAppointments.Count,
                    PatientCount = patientCount,
                    UpcomingAppointments = upcomingAppointments,
                    
                    // Get recent time slots (next 5 days)
                    RecentTimeSlots = await _context.TimeSlots
                        .Where(s => s.DoctorId == doctor.Id)
                        .Where(s => s.StartTime >= DateTime.UtcNow)
                        .OrderBy(s => s.StartTime)
                        .Take(5)
                        .ToListAsync(),
                    
                    // Get today's time slots
                    TodayTimeSlots = await _context.TimeSlots
                        .Where(s => s.DoctorId == doctor.Id)
                        .Where(s => s.StartTime.Date == DateTime.UtcNow.Date)
                        .OrderBy(s => s.StartTime)
                        .ToListAsync(),
                    
                    // Get upcoming time slots (next 7 days)
                    UpcomingTimeSlots = await _context.TimeSlots
                        .Where(s => s.DoctorId == doctor.Id)
                        .Where(s => s.StartTime.Date > DateTime.UtcNow.Date && s.StartTime.Date <= DateTime.UtcNow.Date.AddDays(7))
                        .OrderBy(s => s.StartTime)
                        .ToListAsync(),
                    
                    // Get today's appointments
                    TodayAppointments = await _context.Appointments
                        .Include(a => a.Patient)
                        .Include(a => a.Doctor)
                        .Include(a => a.TreatmentType)
                        .Where(a => a.DoctorId == doctor.Id)
                        .Where(a => a.AppointmentDate.Date == DateTime.UtcNow.Date)
                        .ToListAsync(),
                        
                    // Leave management data
                    LeaveBalances = leaveBalances,
                    PendingLeaveRequests = pendingLeaveRequests.Count,
                    UpcomingLeave = upcomingApprovedLeave
                };

                return View(model);
            }
            catch (Exception ex)
            {
                // Handle any errors gracefully
                ViewData["DoctorName"] = "Doctor";
                ViewBag.ErrorMessage = "Error loading dashboard data: " + ex.Message;
                
                // Return a model with default values
                return View(new DoctorDashboardViewModel());
            }
        }
    }
} 