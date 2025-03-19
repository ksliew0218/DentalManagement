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
    public class ProfileModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;

        public ProfileModel(
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

            [Display(Name = "Address")]
            public string Address { get; set; }

            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Emergency Contact Name")]
            public string EmergencyContactName { get; set; }

            [Display(Name = "Emergency Contact Phone")]
            public string EmergencyContactPhone { get; set; }

            [Display(Name = "Profile Picture")]
            public IFormFile ProfilePicture { get; set; } // 新增头像上传字段
        }

        public string ProfilePictureUrl { get; set; } // 存储当前头像URL

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (user.Role == UserRole.Admin)
            {
                return RedirectToPage("./Index");
            }

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
            if (patient == null)
            {
                return NotFound("Patient profile not found. Please contact the administrator.");
            }

            Input = new InputModel
            {
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Gender = patient.Gender,
                DateOfBirth = patient.DateOfBirth,
                Address = patient.Address,
                PhoneNumber = patient.PhoneNumber,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone
            };

            ProfilePictureUrl = patient.ProfilePic; // 获取当前头像URL

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

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
            if (patient == null)
            {
                return NotFound("Patient profile not found. Please contact the administrator.");
            }

            // 更新患者信息
            patient.FirstName = Input.FirstName;
            patient.LastName = Input.LastName;
            patient.Gender = Input.Gender;
            patient.DateOfBirth = Input.DateOfBirth;
            patient.Address = Input.Address;
            patient.PhoneNumber = Input.PhoneNumber;
            patient.EmergencyContactName = Input.EmergencyContactName;
            patient.EmergencyContactPhone = Input.EmergencyContactPhone;

            // 处理头像上传
            if (Input.ProfilePicture != null)
            {
                var uploadsFolder = Path.Combine("wwwroot", "images", "profiles");
                Directory.CreateDirectory(uploadsFolder); // 确保目录存在

                var uniqueFileName = $"{Guid.NewGuid()}_{Input.ProfilePicture.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ProfilePicture.CopyToAsync(fileStream);
                }

                patient.ProfilePic = $"/images/profiles/{uniqueFileName}";
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
