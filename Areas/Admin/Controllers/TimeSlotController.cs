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
            
            if (_context.TimeSlots == null)
            {
                throw new InvalidOperationException("TimeSlots DbSet is not available.");
            }
            
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

        public async Task<IActionResult> Create()
        {
            var doctors = await _context.Doctors
                .Include(d => d.User)
                .Where(d => d.Status == StatusType.Active && !d.IsDeleted)
                .ToListAsync();
                
            ViewBag.Doctors = new SelectList(doctors, "Id", "User.Email");
            
            var model = new CreateTimeSlotViewModel();
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTimeSlotViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var doctor = await _context.Doctors.FindAsync(model.DoctorId);
                    if (doctor == null || doctor.IsDeleted)
                    {
                        ModelState.AddModelError("DoctorId", "The selected doctor does not exist or has been deleted.");
                        await PrepareViewBagDoctorsAsync();
                        return View(model);
                    }
                    
                    int startHour = model.DailyStartTime.Hour;
                    int endHour = model.DailyEndTime.Hour;
                    
                    if (endHour <= startHour)
                    {
                        ModelState.AddModelError("DailyEndTime", "End time must be after start time");
                        await PrepareViewBagDoctorsAsync();
                        return View(model);
                    }
                    
                    if (model.EndDate < model.StartDate)
                    {
                        ModelState.AddModelError("EndDate", "End date must be after start date");
                        await PrepareViewBagDoctorsAsync();
                        return View(model);
                    }
                    
                    var timeSlots = new List<TimeSlot>();
                    int duplicateSlots = 0;
                    
                    DateTime startDate = DateTime.SpecifyKind(model.StartDate.Date, DateTimeKind.Utc);
                    DateTime endDate = DateTime.SpecifyKind(model.EndDate.Date, DateTimeKind.Utc);
                    
                    var existingSlots = await _context.TimeSlots
                        .Where(ts => ts.DoctorId == model.DoctorId)
                        .Where(ts => ts.StartTime.Date >= startDate && ts.StartTime.Date <= endDate)
                        .ToListAsync();
                    
                    for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        for (int hour = startHour; hour < endHour; hour++)
                        {
                            if (hour == 12) continue;
                            
                            DateTime slotStart = DateTime.SpecifyKind(
                                new DateTime(date.Year, date.Month, date.Day, hour, 0, 0), 
                                DateTimeKind.Utc
                            );
                            
                            DateTime slotEnd = DateTime.SpecifyKind(
                                new DateTime(date.Year, date.Month, date.Day, hour + 1, 0, 0), 
                                DateTimeKind.Utc
                            );
                            
                            bool isDuplicate = existingSlots.Any(s => 
                                s.DoctorId == model.DoctorId && 
                                s.StartTime.Year == slotStart.Year &&
                                s.StartTime.Month == slotStart.Month && 
                                s.StartTime.Day == slotStart.Day &&
                                s.StartTime.Hour == slotStart.Hour);
                            
                            if (!isDuplicate)
                            {
                                timeSlots.Add(new TimeSlot
                                {
                                    DoctorId = model.DoctorId,
                                    StartTime = slotStart,
                                    EndTime = slotEnd,
                                    IsBooked = false
                                });
                            }
                            else
                            {
                                duplicateSlots++;
                            }
                        }
                    }
                    
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
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                }
            }
            
            await PrepareViewBagDoctorsAsync();
            return View(model);
        }
        
        public IActionResult ClearPast()
        {
            return View();
        }
        
        [HttpPost, ActionName("ClearPast")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearPastConfirmed()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                
                var pastSlots = await _context.TimeSlots
                    .Where(t => t.StartTime.Date < today)
                    .ToListAsync();
                
                if (pastSlots.Count == 0)
                {
                    TempData["WarningMessage"] = "No past time slots found to delete.";
                    return RedirectToAction(nameof(Index));
                }
                
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