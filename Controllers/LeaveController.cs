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

namespace DentalManagement.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class LeaveController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly LeaveManagementService _leaveService;

        public LeaveController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            LeaveManagementService leaveService)
        {
            _context = context;
            _userManager = userManager;
            _leaveService = leaveService;
        }

        // GET: Leave
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
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

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == user.Id);
            if (doctor == null)
            {
                return NotFound("Doctor profile not found");
            }

            // Get leave types for dropdown
            ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "Name");

            return View(new LeaveRequestViewModel { DoctorId = doctor.Id });
        }

        // POST: Leave/Apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(LeaveRequestViewModel model)
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

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == user.Id);
            if (doctor == null || doctor.Id != model.DoctorId)
            {
                return NotFound("Doctor profile not found");
            }

            // Create leave request
            var leaveRequest = new DoctorLeaveRequest
            {
                DoctorId = doctor.Id,
                LeaveTypeId = model.LeaveTypeId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Reason = model.Reason,
                Status = LeaveRequestStatus.Pending,
                RequestDate = DateTime.UtcNow
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

        // GET: Leave/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
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
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Leave request cancelled successfully";
            return RedirectToAction(nameof(Index));
        }
    }
} 