using System;
using System.Linq;
using System.Threading.Tasks;
using DentalManagement.Models;
using DentalManagement.Services;
using DentalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class LeaveManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly LeaveManagementService _leaveService;

        public LeaveManagementController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            LeaveManagementService leaveService)
        {
            _context = context;
            _userManager = userManager;
            _leaveService = leaveService;
        }

        public async Task<IActionResult> Index(string status = "Pending")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != UserRole.Admin)
            {
                return RedirectToAction("AccessDenied", "Home", new { area = "" });
            }
            
            if (!Enum.TryParse<LeaveRequestStatus>(status, true, out var leaveStatus))
            {
                leaveStatus = LeaveRequestStatus.Pending;
            }

            var viewModel = new AdminLeaveManagementViewModel();
            
            var allRequests = await _context.DoctorLeaveRequests
                .Include(l => l.Doctor)
                    .ThenInclude(d => d.User)
                .Include(l => l.LeaveType)
                .Include(l => l.ApprovedByUser)
                .OrderByDescending(l => l.RequestDate)
                .ToListAsync();
                
            viewModel.PendingRequests = allRequests.Where(l => l.Status == LeaveRequestStatus.Pending).ToList();
            viewModel.ApprovedRequests = allRequests.Where(l => l.Status == LeaveRequestStatus.Approved).ToList();
            viewModel.RejectedRequests = allRequests.Where(l => l.Status == LeaveRequestStatus.Rejected).ToList();

            ViewBag.CurrentStatus = status;
            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != UserRole.Admin)
            {
                return RedirectToAction("AccessDenied", "Home", new { area = "" });
            }
            
            var leaveRequest = await _context.DoctorLeaveRequests
                .Include(l => l.Doctor)
                    .ThenInclude(d => d.User)
                .Include(l => l.LeaveType)
                .Include(l => l.ApprovedByUser)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            var timeSlots = await _context.TimeSlots
                .Where(ts => ts.DoctorId == leaveRequest.DoctorId &&
                           ts.StartTime.Date >= leaveRequest.StartDate.Date &&
                           ts.StartTime.Date <= leaveRequest.EndDate.Date)
                .ToListAsync();
                
            var bookedTimeSlots = timeSlots.Where(ts => ts.IsBooked).ToList();
            
            ViewBag.TotalTimeSlots = timeSlots.Count;
            ViewBag.BookedTimeSlots = bookedTimeSlots.Count;
            ViewBag.TimeSlots = timeSlots;

            return View(leaveRequest);
        }

        public async Task<IActionResult> Approve(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != UserRole.Admin)
            {
                return RedirectToAction("AccessDenied", "Home", new { area = "" });
            }
            
            var leaveRequest = await _context.DoctorLeaveRequests
                .Include(l => l.Doctor)
                    .ThenInclude(d => d.User)
                .Include(l => l.LeaveType)
                .FirstOrDefaultAsync(l => l.Id == id && l.Status == LeaveRequestStatus.Pending);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            var viewModel = new LeaveApprovalViewModel
            {
                LeaveRequestId = leaveRequest.Id,
                Status = LeaveRequestStatus.Approved
            };

            ViewBag.LeaveRequest = leaveRequest;
            return View("ApproveReject", viewModel);
        }

        public async Task<IActionResult> Reject(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != UserRole.Admin)
            {
                return RedirectToAction("AccessDenied", "Home", new { area = "" });
            }
            
            var leaveRequest = await _context.DoctorLeaveRequests
                .Include(l => l.Doctor)
                    .ThenInclude(d => d.User)
                .Include(l => l.LeaveType)
                .FirstOrDefaultAsync(l => l.Id == id && l.Status == LeaveRequestStatus.Pending);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            var viewModel = new LeaveApprovalViewModel
            {
                LeaveRequestId = leaveRequest.Id,
                Status = LeaveRequestStatus.Rejected
            };

            ViewBag.LeaveRequest = leaveRequest;
            return View("ApproveReject", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReject(LeaveApprovalViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != UserRole.Admin)
            {
                return RedirectToAction("AccessDenied", "Home", new { area = "" });
            }
            
            if (!ModelState.IsValid)
            {
                var leaveRequest = await _context.DoctorLeaveRequests
                    .Include(l => l.Doctor)
                        .ThenInclude(d => d.User)
                    .Include(l => l.LeaveType)
                    .FirstOrDefaultAsync(l => l.Id == model.LeaveRequestId);
                    
                ViewBag.LeaveRequest = leaveRequest;
                return View(model);
            }

            
            if (model.Status == LeaveRequestStatus.Approved)
            {
                var leaveRequest = await _context.DoctorLeaveRequests
                    .FirstOrDefaultAsync(l => l.Id == model.LeaveRequestId);
                
                if (leaveRequest != null)
                {
                    var bookedTimeSlots = await _context.TimeSlots
                        .Where(ts => ts.DoctorId == leaveRequest.DoctorId &&
                                   ts.StartTime.Date >= leaveRequest.StartDate.Date &&
                                   ts.StartTime.Date <= leaveRequest.EndDate.Date &&
                                   ts.IsBooked)
                        .ToListAsync();
                    
                    if (bookedTimeSlots.Any())
                    {
                        TempData["WarningMessage"] = $"Warning: There are {bookedTimeSlots.Count} booked time slots during this leave period. " +
                                                    "You may need to reschedule affected appointments.";
                    }
                }
            }

            var result = await _leaveService.UpdateLeaveRequestStatusAsync(
                model.LeaveRequestId, 
                model.Status, 
                user.Id, 
                model.Comments);

            if (result.Success)
            {
                if (model.Status == LeaveRequestStatus.Approved)
                {
                    var leaveRequest = await _context.DoctorLeaveRequests
                        .Include(l => l.Doctor)
                        .FirstOrDefaultAsync(l => l.Id == model.LeaveRequestId);
                        
                    if (leaveRequest != null)
                    {
                        var affectedTimeSlots = await _context.TimeSlots
                            .CountAsync(ts => ts.DoctorId == leaveRequest.DoctorId &&
                                           ts.StartTime.Date >= leaveRequest.StartDate.Date &&
                                           ts.StartTime.Date <= leaveRequest.EndDate.Date);
                        
                        if (affectedTimeSlots > 0)
                        {
                            TempData["InfoMessage"] = $"{affectedTimeSlots} time slots have been marked as unavailable during the leave period.";
                        }
                    }
                }
                
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ManageBalances(int? doctorId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != UserRole.Admin)
            {
                return RedirectToAction("AccessDenied", "Home", new { area = "" });
            }
            
            if (doctorId.HasValue)
            {
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.Id == doctorId);
                    
                if (doctor == null)
                {
                    return NotFound();
                }
                
                var currentYear = DateTime.UtcNow.Year;
                var leaveBalances = await _leaveService.GetDoctorLeaveBalancesAsync(doctor.Id, currentYear);
                
                ViewBag.Doctor = doctor;
                return View(leaveBalances);
            }
            else
            {
                var doctors = await _context.Doctors
                    .Include(d => d.User)
                    .Where(d => !d.IsDeleted)
                    .OrderBy(d => d.LastName)
                    .ToListAsync();
                    
                return View("SelectDoctor", doctors);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBalance(int id, decimal remainingDays)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != UserRole.Admin)
            {
                return RedirectToAction("AccessDenied", "Home", new { area = "" });
            }
            
            var leaveBalance = await _context.DoctorLeaveBalances
                .Include(lb => lb.Doctor)
                .Include(lb => lb.LeaveType)
                .FirstOrDefaultAsync(lb => lb.Id == id);
                
            if (leaveBalance == null)
            {
                return NotFound();
            }
            
            leaveBalance.RemainingDays = remainingDays;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Leave balance updated successfully";
            return RedirectToAction(nameof(ManageBalances), new { doctorId = leaveBalance.DoctorId });
        }

        public async Task<IActionResult> Initialize(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != UserRole.Admin)
            {
                return RedirectToAction("AccessDenied", "Home", new { area = "" });
            }
            
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);
                
            if (doctor == null)
            {
                return NotFound();
            }
            
            var currentYear = DateTime.UtcNow.Year;
            var existingBalances = await _context.DoctorLeaveBalances
                .AnyAsync(lb => lb.DoctorId == id && lb.Year == currentYear);
                
            if (existingBalances)
            {
                TempData["ErrorMessage"] = "Leave balances already initialized for this doctor.";
                return RedirectToAction(nameof(ManageBalances), new { doctorId = id });
            }
            
            await _leaveService.InitializeDoctorLeaveBalancesAsync(id);
            
            TempData["SuccessMessage"] = "Leave balances initialized successfully.";
            return RedirectToAction(nameof(ManageBalances), new { doctorId = id });
        }
    }
} 