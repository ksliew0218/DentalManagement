using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalManagement.Models;
using DentalManagement.Services;
using DentalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DentalManagement.Controllers
{
    [Authorize]
    public class LeaveController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly LeaveManagementService _leaveService;
        private readonly ILogger<LeaveController> _logger;

        public LeaveController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            LeaveManagementService leaveService,
            ILogger<LeaveController> logger)
        {
            _context = context;
            _userManager = userManager;
            _leaveService = leaveService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.User.Id == userId);
            if (doctor == null)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Doctor" });
            }

            ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
            ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;
            
            var currentYear = DateTime.UtcNow.Year;
            var leaveBalances = await _leaveService.GetDoctorLeaveBalancesAsync(doctor.Id, currentYear);
            
            var leaveRequests = await _leaveService.GetDoctorLeaveRequestsAsync(doctor.Id);

            var viewModel = new DoctorLeaveViewModel
            {
                Doctor = doctor,
                LeaveBalances = leaveBalances,
                LeaveRequests = leaveRequests
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Apply()
        {
            var model = new LeaveRequestViewModel();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.User.Id == userId);
            if (doctor == null)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Doctor" });
            }

            model.DoctorId = doctor.Id;
            ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
            ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;
            
            ViewBag.LeaveTypes = new SelectList(await _context.LeaveTypes.ToListAsync(), "Id", "Name");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(LeaveRequestViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "Name");
                    
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var doctorProfile = await _context.Doctors
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(d => d.User.Id == userId);
                    if (doctorProfile != null)
                    {
                        ViewData["DoctorName"] = $"Dr. {doctorProfile.FirstName} {doctorProfile.LastName}";
                        ViewData["DoctorProfilePicture"] = doctorProfile.ProfilePictureUrl;
                    }
                    
                    return View(model);
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                if (user.Role != UserRole.Doctor)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == user.Id);
                if (doctor == null || doctor.Id != model.DoctorId)
                {
                    return NotFound("Doctor profile not found");
                }

                if (model.EndDate < model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "End date cannot be before start date");
                    ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "Name");
                    ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                    ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;
                    return View(model);
                }

                if (model.StartDate.Date < DateTime.UtcNow.Date)
                {
                    ModelState.AddModelError("StartDate", "Start date cannot be in the past");
                    ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "Name");
                    ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                    ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;
                    return View(model);
                }
                var utcStartDate = DateTime.SpecifyKind(model.StartDate.Date, DateTimeKind.Utc);
                var utcEndDate = DateTime.SpecifyKind(model.EndDate.Date, DateTimeKind.Utc);

                var existingRequest = await _context.DoctorLeaveRequests
                    .AnyAsync(lr => lr.DoctorId == doctor.Id && 
                                   lr.Status == LeaveRequestStatus.Pending &&
                                   ((lr.StartDate <= utcStartDate && lr.EndDate >= utcStartDate) ||
                                    (lr.StartDate <= utcEndDate && lr.EndDate >= utcEndDate) ||
                                    (lr.StartDate >= utcStartDate && lr.EndDate <= utcEndDate)));

                if (existingRequest)
                {
                    ModelState.AddModelError("", "You already have a pending leave request for this date range");
                    ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "Name");
                    ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                    ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;
                    return View(model);
                }

                var leaveRequest = new DoctorLeaveRequest
                {
                    DoctorId = doctor.Id,
                    LeaveTypeId = model.LeaveTypeId,
                    StartDate = DateTime.SpecifyKind(model.StartDate.Date, DateTimeKind.Utc),
                    EndDate = DateTime.SpecifyKind(model.EndDate.Date, DateTimeKind.Utc),
                    Reason = model.Reason ?? "No reason provided",
                    DocumentPath = null, 
                    Status = LeaveRequestStatus.Pending,
                    RequestDate = DateTime.UtcNow,
                    ApprovedById = null, 
                    ApprovalDate = null,
                    Comments = null 
                };

                var result = await _leaveService.ProcessLeaveRequestAsync(leaveRequest);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", result.Message);
                    ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "Name");
                    ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                    ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying for leave");
                ModelState.AddModelError("", "An error occurred while processing your request. Please try again.");
                ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "Name");
                
                var errorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var errorDoctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.User.Id == errorUserId);
                if (errorDoctor != null)
                {
                    ViewData["DoctorName"] = $"Dr. {errorDoctor.FirstName} {errorDoctor.LastName}";
                    ViewData["DoctorProfilePicture"] = errorDoctor.ProfilePictureUrl;
                }
                
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.User.Id == userId);
            if (doctor == null)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Doctor" });
            }

            ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
            ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;
            
            var leaveRequest = await _context.DoctorLeaveRequests
                .Include(l => l.LeaveType)
                .Include(l => l.ApprovedByUser)
                .FirstOrDefaultAsync(l => l.Id == id && l.DoctorId == doctor.Id);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            return View(leaveRequest);
        }

        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.User.Id == userId);
            if (doctor == null)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Doctor" });
            }

            ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
            ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;
            
            var leaveRequest = await _context.DoctorLeaveRequests
                .Include(l => l.LeaveType)
                .FirstOrDefaultAsync(l => l.Id == id && l.DoctorId == doctor.Id);

            if (leaveRequest == null)
            {
                return NotFound("Leave request not found or cannot be cancelled");
            }

            return View(leaveRequest);
        }

        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.User.Id == userId);
            if (doctor == null)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Doctor" });
            }

            var leaveRequest = await _context.DoctorLeaveRequests
                .FirstOrDefaultAsync(l => l.Id == id && l.DoctorId == doctor.Id && l.Status == LeaveRequestStatus.Pending);

            if (leaveRequest == null)
            {
                return NotFound("Leave request not found or cannot be cancelled");
            }

            leaveRequest.Status = LeaveRequestStatus.Rejected;
            leaveRequest.Comments = "Cancelled by doctor";
            leaveRequest.ApprovalDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            leaveRequest.ApprovedById = doctor.User.Id;
            
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Leave request cancelled successfully";
            return RedirectToAction(nameof(Index));
        }
    }
} 