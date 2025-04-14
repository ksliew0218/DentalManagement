using System;
using System.Linq;
using System.Threading.Tasks;
using DentalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LeaveTypesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<LeaveTypesController> _logger;

        public LeaveTypesController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<LeaveTypesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != UserRole.Admin)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var leaveTypes = await _context.LeaveTypes
                .OrderBy(l => l.Name)
                .ToListAsync();

            return View(leaveTypes);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,IsPaid,DefaultDays,Description")] LeaveType leaveType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(leaveType);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Leave type created successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(leaveType);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaveType = await _context.LeaveTypes.FindAsync(id);
            if (leaveType == null)
            {
                return NotFound();
            }
            return View(leaveType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,IsPaid,DefaultDays,Description")] LeaveType leaveType)
        {
            if (id != leaveType.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(leaveType);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Leave type updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LeaveTypeExists(leaveType.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(leaveType);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaveType = await _context.LeaveTypes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (leaveType == null)
            {
                return NotFound();
            }

            return View(leaveType);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var isInUse = await _context.DoctorLeaveRequests.AnyAsync(r => r.LeaveTypeId == id)
                || await _context.DoctorLeaveBalances.AnyAsync(b => b.LeaveTypeId == id);
                
            if (isInUse)
            {
                TempData["ErrorMessage"] = "Cannot delete this leave type as it is being used by leave requests or balances";
                return RedirectToAction(nameof(Index));
            }
            
            var leaveType = await _context.LeaveTypes.FindAsync(id);
            if (leaveType != null)
            {
                _context.LeaveTypes.Remove(leaveType);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Leave type deleted successfully";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool LeaveTypeExists(int id)
        {
            return _context.LeaveTypes.Any(e => e.Id == id);
        }
    }
} 