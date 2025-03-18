using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DentalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using DentalManagement.Authorization;

namespace DentalManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [AdminOnly]
    public class DoctorController : Controller
    {
        private readonly ILogger<DoctorController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public DoctorController(
            ILogger<DoctorController> logger,
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("/Admin/Doctor/Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/Admin/Doctor/Create")]
        public async Task<IActionResult> Create(
            [Bind("FirstName,LastName,Gender,DateOfBirth,PhoneNumber,Specialty,Qualifications,ExperienceYears,ProfilePictureUrl,Status")] 
            Doctor doctor, 
            string Email)
        {
            if (doctor.DateOfBirth.Kind == DateTimeKind.Unspecified)
            {
                doctor.DateOfBirth = DateTime.SpecifyKind(doctor.DateOfBirth, DateTimeKind.Utc);
            }

            var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    return View(doctor);
                }

                await UserCreator.CreateNewUser(_userManager, Email, "P@ssw0rd123!");

                var user = await _userManager.FindByEmailAsync(Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "User creation failed or user not found.");
                    return View(doctor);
                }

                doctor.UserID = user.Id;
                doctor.User = user;

                ModelState.Clear();
                TryValidateModel(doctor);

                if (!ModelState.IsValid)
                {
                    return View(doctor);
                }

                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Doctor created successfully!";
                return RedirectToAction("Index", "Doctor");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Error creating doctor: " + ex.Message;
                return View(doctor);
            }
        }
    }

    public class UserCreator
    {
        public static async Task CreateNewUser(UserManager<User> userManager, string email, string password)
        {
            try
            {
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user = new User
                    {
                        UserName = email,
                        Email = email,
                        Role = UserRole.Doctor,
                        IsActive = true,
                        CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                        UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                    };

                    var result = await userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                    {
                        Console.WriteLine($"Failed to create User: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while creating the user: {ex.Message}");
            }
        }
    }
}
