using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DentalManagement.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DentalManagement.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize]
    public class TimeSlotsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public TimeSlotsController(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.User.Id == user.Id);

                if (doctor == null)
                {
                    return RedirectToAction("AccessDenied", "Home", new { area = "" });
                }

                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;

                var timeSlots = await _context.TimeSlots
                    .Where(ts => ts.DoctorId == doctor.Id)
                    .OrderBy(ts => ts.StartTime)
                    .ToListAsync();

                return View(timeSlots);
            }
            catch (Exception ex)
            {
                ViewData["DoctorName"] = "Doctor";
                ViewBag.ErrorMessage = "Error loading time slots: " + ex.Message;
                return View(Array.Empty<TimeSlot>());
            }
        }
        
        public async Task<IActionResult> Calendar()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.User.Id == user.Id);

                if (doctor == null)
                {
                    return RedirectToAction("AccessDenied", "Home", new { area = "" });
                }

                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                ViewData["DoctorProfilePicture"] = doctor.ProfilePictureUrl;

                var timeSlots = await _context.TimeSlots
                    .Where(ts => ts.DoctorId == doctor.Id)
                    .OrderBy(ts => ts.StartTime)
                    .ToListAsync();

                return View(timeSlots);
            }
            catch (Exception ex)
            {
                ViewData["DoctorName"] = "Doctor";
                ViewBag.ErrorMessage = "Error loading calendar: " + ex.Message;
                return View(Array.Empty<TimeSlot>());
            }
        }
    }
} 