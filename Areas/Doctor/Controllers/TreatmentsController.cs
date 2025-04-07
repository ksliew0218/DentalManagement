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

                // Get all active treatments assigned to this doctor
                var doctorTreatments = await _context.DoctorTreatments
                    .Where(dt => dt.DoctorId == doctor.Id && dt.IsActive && !dt.IsDeleted)
                    .Include(dt => dt.TreatmentType)
                    .ToListAsync();

                // Set doctor name in ViewData for the layout
                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                // Add doctor profile picture URL to ViewData
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
                return NotFound("Treatment not found or not assigned to you.");
            }

            // Get doctor's name for the view
            ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";

            return View(doctorTreatment);
        }
    }
} 