using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DentalManagement.Authorization;
using DentalManagement.Models;

namespace DentalManagement.Controllers
{
    [Authorize]
    [AdminOnly]
    public class DoctorTreatmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DoctorTreatmentController> _logger;
        private readonly UserManager<User> _userManager;

        public DoctorTreatmentController(
            ApplicationDbContext context,
            ILogger<DoctorTreatmentController> logger,
            UserManager<User> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        // GET: DoctorTreatment/ManageAssignments/5 (TreatmentTypeId)
        // GET: DoctorTreatment/ManageAssignments?doctorId=5 (DoctorId)
        public async Task<IActionResult> ManageAssignments(int? id, int? doctorId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                // Check if we have either a treatment type ID or a doctor ID
                if (id == null && doctorId == null)
                {
                    return NotFound();
                }

                // If we have a treatment type ID, show doctor assignments for that treatment
                if (id.HasValue)
                {
                    // Get the treatment type
                    var treatmentType = await _context.TreatmentTypes
                        .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

                    if (treatmentType == null)
                    {
                        return NotFound();
                    }

                    // Get all doctors
                    var doctors = await _context.Doctors
                        .Where(d => d.Status == StatusType.Active && !d.IsDeleted)
                        .OrderBy(d => d.LastName)
                        .ThenBy(d => d.FirstName)
                        .ToListAsync();

                    // Get existing doctor assignments for this treatment
                    var existingAssignments = await _context.DoctorTreatments
                        .Where(dt => dt.TreatmentTypeId == id)
                        .ToListAsync();

                    // Create view model
                    var viewModel = new DoctorTreatmentViewModel
                    {
                        TreatmentType = treatmentType,
                        DoctorAssignments = doctors.Select(d => new DoctorAssignmentViewModel
                        {
                            DoctorId = d.Id,
                            DoctorName = $"Dr. {d.FirstName} {d.LastName}",
                            DoctorSpecialty = d.Specialty,
                            IsAssigned = existingAssignments.Any(ea => ea.DoctorId == d.Id && !ea.IsDeleted),
                            IsActive = existingAssignments.Any(ea => ea.DoctorId == d.Id && ea.IsActive && !ea.IsDeleted),
                            AssignmentId = existingAssignments.FirstOrDefault(ea => ea.DoctorId == d.Id)?.Id ?? 0
                        }).ToList()
                    };

                    ViewBag.ManagementType = "Treatment";
                    return View(viewModel);
                }
                // If we have a doctor ID, show treatment assignments for that doctor
                else
                {
                    // Get the doctor
                    var doctor = await _context.Doctors
                        .FirstOrDefaultAsync(d => d.Id == doctorId && !d.IsDeleted);

                    if (doctor == null)
                    {
                        return NotFound();
                    }

                    // Get all treatment types
                    var treatments = await _context.TreatmentTypes
                        .Where(t => !t.IsDeleted)
                        .OrderBy(t => t.Name)
                        .ToListAsync();

                    // Get existing treatments assigned to this doctor
                    var existingAssignments = await _context.DoctorTreatments
                        .Where(dt => dt.DoctorId == doctorId)
                        .ToListAsync();

                    // Create view model
                    var viewModel = new DoctorTreatmentViewModel
                    {
                        Doctor = doctor,
                        TreatmentAssignments = treatments.Select(t => new TreatmentAssignmentViewModel
                        {
                            TreatmentTypeId = t.Id,
                            TreatmentName = t.Name,
                            TreatmentDuration = t.Duration,
                            TreatmentPrice = t.Price,
                            IsAssigned = existingAssignments.Any(ea => ea.TreatmentTypeId == t.Id && !ea.IsDeleted),
                            IsActive = existingAssignments.Any(ea => ea.TreatmentTypeId == t.Id && ea.IsActive && !ea.IsDeleted),
                            AssignmentId = existingAssignments.FirstOrDefault(ea => ea.TreatmentTypeId == t.Id)?.Id ?? 0
                        }).ToList()
                    };

                    ViewBag.ManagementType = "Doctor";
                    return View("ManageTreatments", viewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading doctor or treatment assignments");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // POST: DoctorTreatment/UpdateAssignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAssignment(int doctorId, int treatmentTypeId, bool isAssigned, bool isActive)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                // Find existing assignment
                var existingAssignment = await _context.DoctorTreatments
                    .FirstOrDefaultAsync(dt => dt.DoctorId == doctorId && dt.TreatmentTypeId == treatmentTypeId);

                if (existingAssignment != null)
                {
                    // Update existing assignment
                    if (!isAssigned)
                    {
                        // Mark as deleted if no longer assigned
                        existingAssignment.IsActive = false;
                        existingAssignment.IsDeleted = true;
                    }
                    else
                    {
                        // Update active status
                        existingAssignment.IsActive = isActive;
                        existingAssignment.IsDeleted = false;
                    }
                    existingAssignment.UpdatedAt = DateTime.UtcNow;
                }
                else if (isAssigned)
                {
                    // Create new assignment
                    _context.DoctorTreatments.Add(new DoctorTreatment
                    {
                        DoctorId = doctorId,
                        TreatmentTypeId = treatmentTypeId,
                        IsActive = isActive,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor assignment");
                return Json(new { success = false, message = "An error occurred while updating the assignment." });
            }
        }

        // GET: DoctorTreatment/ToggleActive/5 (DoctorTreatmentId)
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return Json(new { success = false, message = "Access denied." });
                }

                var doctorTreatment = await _context.DoctorTreatments.FindAsync(id);
                if (doctorTreatment == null)
                {
                    return Json(new { success = false, message = "Assignment not found." });
                }

                // Toggle active status
                doctorTreatment.IsActive = !doctorTreatment.IsActive;
                doctorTreatment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, isActive = doctorTreatment.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling doctor assignment active status");
                return Json(new { success = false, message = "An error occurred while updating the assignment." });
            }
        }

        // POST: DoctorTreatment/ToggleDeleted/5 (DoctorTreatmentId)
        [HttpPost]
        public async Task<IActionResult> ToggleDeleted(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return Json(new { success = false, message = "Access denied." });
                }

                var doctorTreatment = await _context.DoctorTreatments.FindAsync(id);
                if (doctorTreatment == null)
                {
                    return Json(new { success = false, message = "Assignment not found." });
                }

                // Toggle deleted status
                doctorTreatment.IsDeleted = !doctorTreatment.IsDeleted;
                if (doctorTreatment.IsDeleted)
                {
                    doctorTreatment.IsActive = false; // If deleted, also set inactive
                }
                doctorTreatment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, isDeleted = doctorTreatment.IsDeleted, isActive = doctorTreatment.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling doctor assignment deleted status");
                return Json(new { success = false, message = "An error occurred while updating the assignment." });
            }
        }
    }

    // View Models
    public class DoctorTreatmentViewModel
    {
        public TreatmentType TreatmentType { get; set; }
        public Doctor Doctor { get; set; }
        public List<DoctorAssignmentViewModel> DoctorAssignments { get; set; }
        public List<TreatmentAssignmentViewModel> TreatmentAssignments { get; set; }
    }

    public class DoctorAssignmentViewModel
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string DoctorSpecialty { get; set; }
        public bool IsAssigned { get; set; }
        public bool IsActive { get; set; }
        public int AssignmentId { get; set; }
    }

    public class TreatmentAssignmentViewModel
    {
        public int TreatmentTypeId { get; set; }
        public string TreatmentName { get; set; }
        public int TreatmentDuration { get; set; }
        public decimal TreatmentPrice { get; set; }
        public bool IsAssigned { get; set; }
        public bool IsActive { get; set; }
        public int AssignmentId { get; set; }
    }
} 