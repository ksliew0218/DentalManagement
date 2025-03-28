using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using DentalManagement.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Areas.Identity.Pages.Account.Manage
{
    public class DoctorProfileModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;

        public DoctorProfileModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Required]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Required]
            [Display(Name = "Gender")]
            public string Gender { get; set; }

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Date of Birth")]
            public DateTime DateOfBirth { get; set; }

            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; }

            [Required]
            [Display(Name = "Specialty")]
            public string Specialty { get; set; }

            [Display(Name = "Qualifications")]
            public string Qualifications { get; set; }

            [Display(Name = "Years of Experience")]
            [Range(0, 100, ErrorMessage = "Experience years must be between 0 and 100")]
            public int ExperienceYears { get; set; }

            [Display(Name = "Profile Picture")]
            public IFormFile ProfilePicture { get; set; } 
        }

        public string ProfilePictureUrl { get; set; } 

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (user.Role != UserRole.Doctor)
            {
                return RedirectToPage("./Index");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == user.Id);
            if (doctor == null)
            {
                return NotFound("Doctor profile not found. Please contact the administrator.");
            }

            Input = new InputModel
            {
                FirstName = doctor.FirstName,
                LastName = doctor.LastName,
                Gender = doctor.Gender.ToString(),
                DateOfBirth = doctor.DateOfBirth,
                PhoneNumber = doctor.PhoneNumber,
                Specialty = doctor.Specialty,
                Qualifications = doctor.Qualifications,
                ExperienceYears = doctor.ExperienceYears
            };

            ProfilePictureUrl = doctor.ProfilePictureUrl;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
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

            doctor.FirstName = Input.FirstName;
            doctor.LastName = Input.LastName;
            
            if (Enum.TryParse<GenderType>(Input.Gender, out var gender))
            {
                doctor.Gender = gender;
            }
            
            doctor.DateOfBirth = Input.DateOfBirth;
            doctor.PhoneNumber = Input.PhoneNumber;
            doctor.Specialty = Input.Specialty;
            doctor.Qualifications = Input.Qualifications;
            doctor.ExperienceYears = Input.ExperienceYears;

            if (Input.ProfilePicture != null)
            {
                var uploadsFolder = Path.Combine("/app", "wwwroot", "images", "profiles");
                Directory.CreateDirectory(uploadsFolder); 

                var uniqueFileName = $"{Guid.NewGuid()}_{Input.ProfilePicture.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ProfilePicture.CopyToAsync(fileStream);
                }

                doctor.ProfilePictureUrl = $"/images/profiles/{uniqueFileName}";
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
                return Page();
            }

            return RedirectToPage();
        }
    }
} 