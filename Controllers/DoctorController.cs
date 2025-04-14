using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DentalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using DentalManagement.Authorization;
using System.IO;
using DentalManagement.Services;

namespace DentalManagement.Controllers
{
    [Authorize]
    [AdminOnly]
    public class DoctorController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DoctorController> _logger;
        private readonly LeaveManagementService _leaveService;

        public DoctorController(
            UserManager<User> userManager,
            ApplicationDbContext context,
            ILogger<DoctorController> logger,
            LeaveManagementService leaveService)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _leaveService = leaveService;
        }

        public async Task<IActionResult> Index()
        {
            var doctors = await _context.Doctors
                .Include(d => d.User)
                .Where(d => !d.IsDeleted) 
                .ToListAsync();

            return View(doctors);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("FirstName,LastName,Gender,DateOfBirth,PhoneNumber,Specialty,Qualifications,ExperienceYears,Status")] 
            Doctor doctor, 
            string Email,
            IFormFile ProfileImage)
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

                var user = new User
                {
                    UserName = Email,
                    Email = Email,
                    Role = UserRole.Doctor,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, "P@ssw0rd123!");
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", "Failed to create user.");
                    return View(doctor);
                }

                doctor.UserID = user.Id;
                doctor.User = user;

                if (ProfileImage != null && ProfileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine("/app", "wwwroot", "images", "profiles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}_{ProfileImage.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProfileImage.CopyToAsync(stream);
                    }

                    doctor.ProfilePictureUrl = $"/images/profiles/{uniqueFileName}";
                }

                ModelState.Clear();
                TryValidateModel(doctor);

                if (!ModelState.IsValid)
                {
                    return View(doctor);
                }

                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();
                
                await _leaveService.InitializeDoctorLeaveBalancesAsync(doctor.Id);

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Doctor created successfully with leave balances initialized!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Error creating doctor: " + ex.Message;
                return View(doctor);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (doctor == null)
            {
                return NotFound();
            }

            if (doctor.Status == StatusType.Active)
            {
                doctor.Status = StatusType.Inactive;
                if (doctor.User != null) doctor.User.IsActive = false;
                TempData["SuccessMessage"] = "Doctor Deactivated successfully.";
                TempData["AlertType"] = "danger"; 
            }
            else
            {
                doctor.Status = StatusType.Active;
                if (doctor.User != null) doctor.User.IsActive = true;
                TempData["SuccessMessage"] = "Doctor Activated successfully.";
                TempData["AlertType"] = "success"; 
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);

            if (doctor == null)
            {
                return NotFound();
            }

            doctor.IsDeleted = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Doctor deleted successfully.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.DoctorTreatments)
                    .ThenInclude(dt => dt.TreatmentType)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        public async Task<IActionResult> Details(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.DoctorTreatments)
                    .ThenInclude(dt => dt.TreatmentType)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (doctor == null)
            {
                return NotFound();
            }

            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.TreatmentType)
                .Where(a => a.DoctorId == id && 
                           a.AppointmentDate >= DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc) && 
                           a.Status != "Cancelled" && 
                           a.Status != "Completed")
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .Take(5) 
                .ToListAsync();

            ViewBag.UpcomingAppointments = upcomingAppointments;

            return View(doctor);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, 
            [Bind("Id,UserID,FirstName,LastName,Gender,DateOfBirth,PhoneNumber,Specialty,Qualifications,ExperienceYears,Status,IsDeleted")] 
            Doctor doctor, 
            IFormFile ProfileImage)
        {
            if (id != doctor.Id || doctor.IsDeleted)
            {
                return NotFound();
            }

            var existingDoctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (existingDoctor == null)
            {
                return NotFound();
            }

            try
            {
                existingDoctor.FirstName = doctor.FirstName;
                existingDoctor.LastName = doctor.LastName;
                existingDoctor.Gender = doctor.Gender;
                if (doctor.DateOfBirth != null)
                {
                    existingDoctor.DateOfBirth = DateTime.SpecifyKind(doctor.DateOfBirth, DateTimeKind.Utc);
                }
                existingDoctor.PhoneNumber = doctor.PhoneNumber;
                existingDoctor.Specialty = doctor.Specialty;
                existingDoctor.Qualifications = doctor.Qualifications;
                existingDoctor.ExperienceYears = doctor.ExperienceYears;
                existingDoctor.Status = doctor.Status;

                if (ProfileImage != null && ProfileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine("/app", "wwwroot", "images", "profiles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}_{ProfileImage.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProfileImage.CopyToAsync(stream);
                    }

                    existingDoctor.ProfilePictureUrl = $"/images/profiles/{uniqueFileName}";
                }

                Console.WriteLine($"Updating Doctor ID: {existingDoctor.Id}, UserID: {existingDoctor.UserID}");
                Console.WriteLine($"New Name: {existingDoctor.FirstName} {existingDoctor.LastName}");

                _context.Update(existingDoctor);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Doctor details updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating doctor: " + ex.Message;
                return View(doctor);
            }
        }
    }
}
