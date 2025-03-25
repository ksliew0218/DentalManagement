using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalManagement.Models;
using DentalManagement.ViewModels;
using DentalManagement.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DentalManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    [Authorize(Policy = "AdminOnly")]
    public class TimeSlotController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TimeSlotController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            
            // Check if the TimeSlots DbSet is available
            if (_context.TimeSlots == null)
            {
                throw new InvalidOperationException("TimeSlots DbSet is not available.");
            }
            
            // Check if context can connect to database
            try
            {
                bool canConnect = _context.Database.CanConnect();
                Console.WriteLine($"Database connection test: {(canConnect ? "Successful" : "Failed")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing database connection: {ex.Message}");
            }
        }

        // GET: Admin/TimeSlot/Index
        public async Task<IActionResult> Index()
        {
            var timeSlots = await _context.TimeSlots
                .Include(t => t.Doctor)
                .ThenInclude(d => d.User)
                .Where(t => !t.Doctor.IsDeleted)
                .OrderBy(t => t.StartTime)
                .ToListAsync();
                
            return View(timeSlots);
        }

        // GET: Admin/TimeSlot/Create
        public async Task<IActionResult> Create()
        {
            var doctors = await _context.Doctors
                .Include(d => d.User)
                .Where(d => d.Status == StatusType.Active && !d.IsDeleted)
                .ToListAsync();
                
            ViewBag.Doctors = new SelectList(doctors, "Id", "User.Email");
            
            // The default values are now set in the CreateTimeSlotViewModel constructor
            var model = new CreateTimeSlotViewModel();
            
            return View(model);
        }

        // POST: Admin/TimeSlot/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTimeSlotViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if doctor exists and is not deleted
                var doctor = await _context.Doctors.FindAsync(model.DoctorId);
                if (doctor == null || doctor.IsDeleted)
                {
                    ModelState.AddModelError("DoctorId", "The selected doctor does not exist or has been deleted.");
                    await PrepareViewBagDoctorsAsync();
                    return View(model);
                }
                
                // Get the time components from DailyStartTime and DailyEndTime
                TimeSpan startTimeOfDay = model.DailyStartTime.TimeOfDay;
                TimeSpan endTimeOfDay = model.DailyEndTime.TimeOfDay;
                
                // Validate that end time is after start time
                if (endTimeOfDay <= startTimeOfDay)
                {
                    ModelState.AddModelError("DailyEndTime", "End time must be after start time");
                    await PrepareViewBagDoctorsAsync();
                    return View(model);
                }
                
                // Validate that the date range is valid
                if (model.EndDate < model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "End date must be after start date");
                    await PrepareViewBagDoctorsAsync();
                    return View(model);
                }
                
                // Define lunch time (12:00 PM - 1:00 PM)
                TimeSpan lunchTimeStart = new TimeSpan(12, 0, 0); // 12:00 PM
                TimeSpan lunchTimeEnd = new TimeSpan(13, 0, 0);   // 1:00 PM
                
                // Calculate all slots for each day in the date range
                var timeSlots = new List<TimeSlot>();
                int duplicateSlots = 0;
                
                // Get existing slots for this doctor in the date range to check for duplicates
                var existingSlots = await _context.TimeSlots
                    .Where(ts => ts.DoctorId == model.DoctorId)
                    .Where(ts => ts.StartTime.Date >= model.StartDate.Date && ts.StartTime.Date <= model.EndDate.Date)
                    .ToListAsync();
                
                for (DateTime date = model.StartDate.Date; date <= model.EndDate.Date; date = date.AddDays(1))
                {
                    // Create DateTime objects with UTC Kind
                    DateTime currentStart = DateTime.SpecifyKind(
                        date.Add(startTimeOfDay), 
                        DateTimeKind.Utc
                    );
                    
                    DateTime currentEnd = DateTime.SpecifyKind(
                        date.Add(startTimeOfDay).AddHours(1), 
                        DateTimeKind.Utc
                    );
                    
                    DateTime dayEnd = DateTime.SpecifyKind(
                        date.Add(endTimeOfDay), 
                        DateTimeKind.Utc
                    );
                    
                    // Create 1-hour slots for this day
                    while (currentEnd <= dayEnd)
                    {
                        // Skip slot creation during lunch time (12:00 PM - 1:00 PM)
                        TimeSpan currentTimeOfDay = currentStart.TimeOfDay;
                        if (currentTimeOfDay < lunchTimeStart || currentTimeOfDay >= lunchTimeEnd)
                        {
                            // Check if this slot already exists
                            bool isDuplicate = existingSlots.Any(s => 
                                s.DoctorId == model.DoctorId && 
                                s.StartTime.Year == currentStart.Year &&
                                s.StartTime.Month == currentStart.Month && 
                                s.StartTime.Day == currentStart.Day &&
                                s.StartTime.Hour == currentStart.Hour);
                            
                            if (!isDuplicate)
                            {
                                timeSlots.Add(new TimeSlot
                                {
                                    DoctorId = model.DoctorId,
                                    StartTime = currentStart,
                                    EndTime = currentEnd,
                                    IsBooked = false
                                });
                            }
                            else
                            {
                                duplicateSlots++;
                            }
                        }
                        
                        // Move to the next hour
                        currentStart = currentEnd;
                        currentEnd = DateTime.SpecifyKind(
                            currentStart.AddHours(1), 
                            DateTimeKind.Utc
                        );
                    }
                }
                
                try
                {
                    if (timeSlots.Count == 0)
                    {
                        if (duplicateSlots > 0)
                        {
                            TempData["WarningMessage"] = $"All slots already exist for the selected date range and doctor. {duplicateSlots} duplicate slots were skipped.";
                        }
                        else
                        {
                            TempData["WarningMessage"] = "No slots were created. Check your time range and lunch break settings.";
                        }
                        
                        return RedirectToAction(nameof(Index));
                    }
                    
                    // Save all time slots to database
                    _context.TimeSlots.AddRange(timeSlots);
                    await _context.SaveChangesAsync();
                    
                    string message = $"Successfully created {timeSlots.Count} time slots (lunch break 12-1pm excluded).";
                    if (duplicateSlots > 0)
                    {
                        message += $" {duplicateSlots} duplicate slots were skipped.";
                    }
                    
                    TempData["SuccessMessage"] = message;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving time slots: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while saving time slots. Please try again.");
                }
            }
            
            await PrepareViewBagDoctorsAsync();
            return View(model);
        }
        
        // GET: Admin/TimeSlot/ClearPast
        public IActionResult ClearPast()
        {
            return View();
        }
        
        // POST: Admin/TimeSlot/ClearPast
        [HttpPost, ActionName("ClearPast")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearPastConfirmed()
        {
            try
            {
                // Get the current date in UTC
                var today = DateTime.UtcNow.Date;
                
                // Find all past time slots
                var pastSlots = await _context.TimeSlots
                    .Where(t => t.StartTime.Date < today)
                    .ToListAsync();
                
                if (pastSlots.Count == 0)
                {
                    TempData["WarningMessage"] = "No past time slots found to delete.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Remove the past slots
                _context.TimeSlots.RemoveRange(pastSlots);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Successfully deleted {pastSlots.Count} past time slots.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing past time slots: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while clearing past time slots.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // Helper method to prepare ViewBag.Doctors
        private async Task PrepareViewBagDoctorsAsync()
        {
            var doctors = await _context.Doctors
                .Include(d => d.User)
                .Where(d => d.Status == StatusType.Active && !d.IsDeleted)
                .ToListAsync();
                
            ViewBag.Doctors = new SelectList(doctors, "Id", "User.Email");
        }
    }
} 