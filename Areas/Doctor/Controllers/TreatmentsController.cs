using DentalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace DentalManagement.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize]
    public class TreatmentsController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public TreatmentsController(
            UserManager<User> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserID == user.Id);
                
                if (doctor == null)
                {
                    return NotFound("Doctor profile not found. Please contact the administrator.");
                }

                var doctorTreatments = await _context.DoctorTreatments
                    .Where(dt => dt.DoctorId == doctor.Id && dt.IsActive && !dt.IsDeleted)
                    .Include(dt => dt.TreatmentType)
                    .ToListAsync();

                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;

                return View(doctorTreatments);
            }
            catch (Exception ex)
            {
                return Content($"Error loading treatments: {ex.Message}");
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try 
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserID == user.Id);
                
                if (doctor == null)
                {
                    return NotFound("Doctor profile not found. Please contact the administrator.");
                }

                var doctorTreatment = await _context.DoctorTreatments
                    .Include(dt => dt.TreatmentType)
                    .FirstOrDefaultAsync(dt => dt.Id == id && dt.DoctorId == doctor.Id && dt.IsActive && !dt.IsDeleted);

                if (doctorTreatment == null)
                {
                    var alternativeTreatment = await _context.DoctorTreatments
                        .FirstOrDefaultAsync(dt => dt.TreatmentTypeId == id && dt.DoctorId == doctor.Id && dt.IsActive && !dt.IsDeleted);
                    
                    if (alternativeTreatment != null)
                    {
                        return RedirectToAction("TreatmentDetails", new { treatmentTypeId = id });
                    }
                    
                    return NotFound("Treatment not found or not assigned to you.");
                }

                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";

                return View(doctorTreatment);
            }
            catch (Exception ex)
            {
                return Content($"Error retrieving treatment details: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("TreatmentDetails/{treatmentTypeId}")]
        public async Task<IActionResult> TreatmentDetails(int treatmentTypeId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserID == user.Id);
                
                if (doctor == null)
                {
                    return NotFound("Doctor profile not found. Please contact the administrator.");
                }

                var doctorTreatment = await _context.DoctorTreatments
                    .Include(dt => dt.TreatmentType)
                    .FirstOrDefaultAsync(dt => dt.TreatmentTypeId == treatmentTypeId && dt.DoctorId == doctor.Id && dt.IsActive && !dt.IsDeleted);

                if (doctorTreatment == null)
                {
                    return NotFound("Treatment not found or not assigned to you.");
                }

                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";

                return View("Details", doctorTreatment);
            }
            catch (Exception ex)
            {
                return Content($"Error retrieving treatment details: {ex.Message}");
            }
        }
    }
} 