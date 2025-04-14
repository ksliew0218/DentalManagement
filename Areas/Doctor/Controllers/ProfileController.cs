using DentalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace DentalManagement.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            try 
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    return Content($"User not found. User claims ID: {userId ?? "none"}");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var rolesList = string.Join(", ", roles);

                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == user.Id);
                if (doctor == null)
                {
                    return Content($"Doctor profile not found for user {user.Id}. User has roles: {rolesList}");
                }

                var model = new Models.DoctorProfileViewModel
                {
                    FirstName = doctor.FirstName,
                    LastName = doctor.LastName,
                    Gender = doctor.Gender.ToString(),
                    DateOfBirth = doctor.DateOfBirth,
                    PhoneNumber = doctor.PhoneNumber,
                    Specialty = doctor.Specialty,
                    Qualifications = doctor.Qualifications,
                    ExperienceYears = doctor.ExperienceYears,
                    ProfilePictureUrl = doctor.ProfilePictureUrl
                };
                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;

                return View(model);
            }
            catch (Exception ex)
            {
                return Content($"Error loading profile: {ex.Message}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Models.DoctorProfileViewModel model, IFormFile? profilePicture)
        {
            if (ModelState.ContainsKey("ProfilePictureUrl"))
            {
                ModelState.Remove("ProfilePictureUrl");
            }
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == user.Id);
            if (doctor == null)
            {
                return NotFound("Doctor profile not found. Please contact the administrator.");
            }

            doctor.FirstName = model.FirstName;
            doctor.LastName = model.LastName;
            
            if (Enum.TryParse<GenderType>(model.Gender, out var gender))
            {
                doctor.Gender = gender;
            }
            
            doctor.DateOfBirth = model.DateOfBirth;
            doctor.PhoneNumber = model.PhoneNumber;
            doctor.Specialty = model.Specialty;
            doctor.Qualifications = model.Qualifications;
            doctor.ExperienceYears = model.ExperienceYears;

            if (profilePicture != null && profilePicture.Length > 0)
            {
                try
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
                    
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}_{profilePicture.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(fileStream);
                    }

                    doctor.ProfilePictureUrl = $"/images/profiles/{uniqueFileName}";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error uploading profile picture: {ex.Message}");
                    ModelState.AddModelError("profilePicture", "Failed to upload profile picture, but other profile information was updated.");
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                await _signInManager.RefreshSignInAsync(user);
                TempData["StatusMessage"] = "Your profile has been updated successfully.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while updating your profile. Please try again.");
                Console.WriteLine($"Error updating profile: {ex.Message}");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult ChangePassword()
        {
            return RedirectToPage("/Account/Manage/ChangePassword", new { area = "Identity" });
        }
    }
} 