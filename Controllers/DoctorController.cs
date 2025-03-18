using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DentalManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Controllers
{
    public class DoctorController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DoctorController> _logger;

        public DoctorController(
            UserManager<User> userManager,
            ApplicationDbContext context,
            ILogger<DoctorController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        // GET: Doctor/Index
        public async Task<IActionResult> Index()
        {
            var doctors = await _context.Doctors
                .Include(d => d.User) // Ensure we get user details
                .ToListAsync();

            return View(doctors);
        }

        // GET: Doctor/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Doctor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Role = UserRole.Doctor,
                    IsActive = true // Ensure user is active by default
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Doctor account created successfully.");

                    var createdUser = await _userManager.FindByEmailAsync(model.Email);
                    if (createdUser == null)
                    {
                        ModelState.AddModelError(string.Empty, "User creation failed. Try again.");
                        return View(model);
                    }

                    var doctor = new Doctor
                    {
                        UserID = createdUser.Id,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Specialty = model.Specialty,
                        PhoneNumber = model.PhoneNumber,
                        Status = StatusType.Active // Default status
                    };

                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Doctor details saved successfully.");
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // POST: Doctor/ToggleStatus
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User) // Include User to modify IsActive as well
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            if (doctor.Status == StatusType.Active)
            {
                doctor.Status = StatusType.Inactive;
                if (doctor.User != null) doctor.User.IsActive = false; // Deactivate user
                TempData["SuccessMessage"] = "Doctor deactivated successfully.";
            }
            else
            {
                doctor.Status = StatusType.Active;
                if (doctor.User != null) doctor.User.IsActive = true; // Activate user
                TempData["SuccessMessage"] = "Doctor activated successfully.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
