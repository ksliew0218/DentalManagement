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

        // GET: Leave
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Check if user is a doctor
            if (user.Role != UserRole.Doctor)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == user.Id);
            if (doctor == null)
            {
                return NotFound("Doctor profile not found");
            }

            // Get current year leave balances
            var currentYear = DateTime.UtcNow.Year;
            var leaveBalances = await _leaveService.GetDoctorLeaveBalancesAsync(doctor.Id, currentYear);
            
            // Get leave requests
            var leaveRequests = await _leaveService.GetDoctorLeaveRequestsAsync(doctor.Id);

            var viewModel = new DoctorLeaveViewModel
            {
                Doctor = doctor,
                LeaveBalances = leaveBalances,
                LeaveRequests = leaveRequests
            };

            return View(viewModel);
        }

        // GET: Leave/Apply
        public async Task<IActionResult> Apply()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Check if user is a doctor
            if (user.Role != UserRole.Doctor)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == user.Id);
            if (doctor == null)
            {
                return NotFound("Doctor profile not found");
            }

            // Initialize with default dates
            ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "Name");
            return View(new LeaveRequestViewModel
            {
                DoctorId = doctor.Id,
                StartDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc)
            });
        }

        // POST: Leave/Apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(LeaveRequestViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "Name");
                    return View(model);
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                // Check if user is a doctor
                if (user.Role != UserRole.Doctor)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == user.Id);
                if (doctor == null || doctor.Id != model.DoctorId)
                {
                    return NotFound("Doctor profile not found");
                }

                // Validate dates
                if (model.EndDate < model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "End date cannot be before start date");
                    ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "Name");
                    return View(model);
                }

                if (model.StartDate.Date < DateTime.UtcNow.Date)
                {
                    ModelState.AddModelError("StartDate", "Start date cannot be in the past");
                    ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "Name");
                    return View(model);
                }

                // Normalize dates to UTC for duplicate check
                var utcStartDate = DateTime.SpecifyKind(model.StartDate.Date, DateTimeKind.Utc);
                var utcEndDate = DateTime.SpecifyKind(model.EndDate.Date, DateTimeKind.Utc);

                // Check for duplicate leave requests
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
                    return View(model);
                }

                // Create leave request
                var leaveRequest = new DoctorLeaveRequest
                {
                    DoctorId = doctor.Id,
                    LeaveTypeId = model.LeaveTypeId,
                    StartDate = DateTime.SpecifyKind(model.StartDate.Date, DateTimeKind.Utc),
                    EndDate = DateTime.SpecifyKind(model.EndDate.Date, DateTimeKind.Utc),
                    Reason = model.Reason ?? "No reason provided",
                    DocumentPath = null, // Document is optional
                    Status = LeaveRequestStatus.Pending,
                    RequestDate = DateTime.UtcNow,
                    ApprovedById = null, // Will be set when approved/rejected
                    ApprovalDate = null,
                    Comments = null // Will be set when approved/rejected
                };

                // Process the request
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
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying for leave");
                ModelState.AddModelError("", "An error occurred while processing your request. Please try again.");
                ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "Name");
                return View(model);
            }
        }

        // GET: Leave/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Check if user is a doctor
            if (user.Role != UserRole.Doctor)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == user.Id);
            if (doctor == null)
            {
                return NotFound("Doctor profile not found");
            }

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

        // GET: Leave/Cancel/5
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Check if user is a doctor
            if (user.Role != UserRole.Doctor)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == user.Id);
            if (doctor == null)
            {
                return NotFound("Doctor profile not found");
            }

            var leaveRequest = await _context.DoctorLeaveRequests
                .Include(l => l.LeaveType)
                .FirstOrDefaultAsync(l => l.Id == id && l.DoctorId == doctor.Id && l.Status == LeaveRequestStatus.Pending);

            if (leaveRequest == null)
            {
                return NotFound("Leave request not found or cannot be cancelled");
            }

            return View(leaveRequest);
        }

        // POST: Leave/Cancel/5
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Check if user is a doctor
            if (user.Role != UserRole.Doctor)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == user.Id);
            if (doctor == null)
            {
                return NotFound("Doctor profile not found");
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
            leaveRequest.ApprovedById = user.Id;
            
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Leave request cancelled successfully";
            return RedirectToAction(nameof(Index));
        }
    }
} 