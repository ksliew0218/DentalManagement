using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DentalManagement.Authorization;
using Microsoft.EntityFrameworkCore;
using DentalManagement.Models;
using DentalManagement.Areas.Patient.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using DentalManagement.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DentalManagement.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize]
    [PatientOnly]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AppointmentsController> _logger;
        private readonly IEmailService _emailService;
        private readonly IPaymentService _paymentService; // Add this line


        public AppointmentsController(
            ApplicationDbContext context, 
            UserManager<User> userManager,
            ILogger<AppointmentsController> logger,
            IPaymentService paymentService,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
            _paymentService = paymentService;
        }

        // GET: Patient/Appointments
        public async Task<IActionResult> Index()
        {
            var viewModel = new AppointmentsListViewModel
            {
                UpcomingAppointments = new List<AppointmentViewModel>(),
                PastAppointments = new List<AppointmentViewModel>(),
                CancelledAppointments = new List<AppointmentViewModel>()
            };

            try
            {
                // Get current user
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    // Get patient
                    var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
                    if (patient != null)
                    {
                        // Get the current date and time
                        var now = DateTime.UtcNow;
                        var today = now.Date;

                        // Get all appointments for this patient
                        var allAppointments = await _context.Appointments
                            .Include(a => a.Doctor)
                            .Include(a => a.TreatmentType)
                            .Where(a => a.PatientId == patient.Id)
                            .OrderBy(a => a.AppointmentDate)
                            .ThenBy(a => a.AppointmentTime)
                            .ToListAsync();

                        // Process each appointment and categorize it
                        foreach (var appointment in allAppointments)
                        {
                            var appointmentViewModel = new AppointmentViewModel
                            {
                                Id = appointment.Id,
                                TreatmentName = appointment.TreatmentType.Name,
                                DoctorName = $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}",
                                AppointmentDate = appointment.AppointmentDate,
                                AppointmentTime = appointment.AppointmentTime,
                                Status = appointment.Status,
                                PaymentStatus = appointment.PaymentStatus,
                                // Can cancel if not already cancelled or completed, and appointment is in the future
                                CanCancel = appointment.Status != "Cancelled" && 
                                        appointment.Status != "Completed" && 
                                        (appointment.AppointmentDate > today || 
                                        (appointment.AppointmentDate == today && 
                                            appointment.AppointmentTime > now.TimeOfDay))
                            };

                            // Categorize appointments
                            if (appointment.Status == "Cancelled")
                            {
                                // All cancelled appointments go to the cancelled tab
                                viewModel.CancelledAppointments.Add(appointmentViewModel);
                            }
                            else if (appointment.Status == "Completed")
                            {
                                // All completed appointments go to the past tab
                                viewModel.PastAppointments.Add(appointmentViewModel);
                            }
                            else
                            {
                                // Check if appointment is in the past based on date and time
                                bool isPastAppointment = appointment.AppointmentDate < today || 
                                                    (appointment.AppointmentDate == today && 
                                                        appointment.AppointmentTime < now.TimeOfDay);

                                if (isPastAppointment)
                                {
                                    viewModel.PastAppointments.Add(appointmentViewModel);
                                }
                                else
                                {
                                    viewModel.UpcomingAppointments.Add(appointmentViewModel);
                                }
                            }
                        }

                        // Sort the appointment collections
                        viewModel.UpcomingAppointments = viewModel.UpcomingAppointments
                            .OrderBy(a => a.AppointmentDate)
                            .ThenBy(a => a.AppointmentTime)
                            .ToList();

                        viewModel.PastAppointments = viewModel.PastAppointments
                            .OrderByDescending(a => a.AppointmentDate)
                            .ThenByDescending(a => a.AppointmentTime)
                            .ToList();

                        viewModel.CancelledAppointments = viewModel.CancelledAppointments
                            .OrderByDescending(a => a.AppointmentDate)
                            .ThenByDescending(a => a.AppointmentTime)
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error using proper logging
                _logger.LogError(ex, "Error fetching appointments");
            }
            
            return View("_MyAppointments", viewModel);
        }
        
        // GET: Patient/Appointments/Book
        public async Task<IActionResult> Book()
        {
            // Get active treatment types from database
            var treatments = await _context.TreatmentTypes
                .Where(t => t.IsActive && !t.IsDeleted)
                .Select(t => new TreatmentViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description ?? "",
                    Price = t.Price,
                    Duration = t.Duration,
                    ImageUrl = t.ImageUrl
                })
                .ToListAsync();
            
            // Use these fixed categories or dynamically determine them
            var categories = new List<string> { "Cleaning", "Cosmetic", "Restorative", "Surgical", "Orthodontic" };
            
            var viewModel = new TreatmentSelectionViewModel
            {
                Categories = categories,
                Treatments = treatments
            };
            
            return View(viewModel);
        }
        
        // POST: Patient/Appointments/SaveTreatmentSelection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveTreatmentSelection(int treatmentId, string redirectUrl)
        {
            // Extensive logging
            _logger.LogInformation($"SaveTreatmentSelection START");
            _logger.LogInformation($"Treatment ID: {treatmentId}");
            _logger.LogInformation($"Redirect URL: {redirectUrl}");

            try 
            {
                var treatmentExists = _context.TreatmentTypes
                    .Any(t => t.Id == treatmentId && t.IsActive && !t.IsDeleted);
                
                _logger.LogInformation($"Treatment exists: {treatmentExists}");

                if (!treatmentExists)
                {
                    _logger.LogWarning($"Treatment with ID {treatmentId} not found or inactive");
                    return RedirectToAction("Book");
                }

                HttpContext.Session.SetInt32("SelectedTreatmentId", treatmentId);
                
                _logger.LogInformation("Treatment ID stored in session");

                var redirectPath = redirectUrl ?? "/Patient/Appointments/Book/Doctor";
                _logger.LogInformation($"Redirecting to: {redirectPath}");

                return Redirect(redirectPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveTreatmentSelection");
                return RedirectToAction("Book");
            }
            finally 
            {
                _logger.LogInformation("SaveTreatmentSelection END");
            }
        }
        
        // GET: Patient/Appointments/Book/Doctor
        public async Task<IActionResult> Doctor()
        {
            // Get the selected treatment ID from session
            var selectedTreatmentId = HttpContext.Session.GetInt32("SelectedTreatmentId");
            
            if (selectedTreatmentId == null)
            {
                // Try to get from query string (for direct navigation)
                if (int.TryParse(Request.Query["treatmentId"], out int queryTreatmentId))
                {
                    selectedTreatmentId = queryTreatmentId;
                    HttpContext.Session.SetInt32("SelectedTreatmentId", queryTreatmentId);
                }
                else
                {
                    // If no treatment is selected, redirect back to treatment selection
                    return RedirectToAction("Book");
                }
            }
            
            // Get doctors who are associated with the selected treatment
            var doctorQuery = from dt in _context.DoctorTreatments
                             where dt.TreatmentTypeId == selectedTreatmentId && dt.IsActive && !dt.IsDeleted
                             join d in _context.Doctors on dt.DoctorId equals d.Id
                             where d.Status == StatusType.Active && !d.IsDeleted
                             select new DoctorViewModel
                             {
                                 Id = d.Id,
                                 Name = "Dr. " + d.FirstName + " " + d.LastName,
                                 Specialization = d.Specialty,
                                 YearsOfExperience = d.ExperienceYears,
                                 ProfileImageUrl = d.ProfilePictureUrl ?? "/images/doctors/doctor-placeholder.png",
                                 Qualifications = d.Qualifications
                             };
            
            var doctors = await doctorQuery.ToListAsync();
            
            // If no doctors are found for this treatment, get all active doctors as fallback
            if (!doctors.Any())
            {
                doctors = await _context.Doctors
                    .Where(d => d.Status == StatusType.Active && !d.IsDeleted)
                    .Select(d => new DoctorViewModel
                    {
                        Id = d.Id,
                        Name = "Dr. " + d.FirstName + " " + d.LastName,
                        Specialization = d.Specialty,
                        YearsOfExperience = d.ExperienceYears,
                        ProfileImageUrl = d.ProfilePictureUrl ?? "/images/doctors/doctor-placeholder.png",
                    })
                    .ToListAsync();
            }
            
            // Get the treatment details to display in the view
            var treatment = await _context.TreatmentTypes
                .Where(t => t.Id == selectedTreatmentId)
                .FirstOrDefaultAsync();
                
            if (treatment != null)
            {
                ViewData["TreatmentName"] = treatment.Name;
                ViewData["TreatmentDuration"] = $"{treatment.Duration} min";
                ViewData["TreatmentPrice"] = $"RM {treatment.Price}";
                
                // Store treatment details in TempData
                TempData["SelectedTreatment"] = JsonConvert.SerializeObject(new TreatmentViewModel
                {
                    Id = treatment.Id,
                    Name = treatment.Name,
                    Description = treatment.Description ?? "",
                    Price = treatment.Price,
                    Duration = treatment.Duration
                });
            }
            
            return View(doctors);
        }
        
        // POST: Patient/Appointments/SaveDoctorSelection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveDoctorSelection(int doctorId, string redirectUrl)
        {
            // Validate that the doctor exists and is active
            var doctorExists = _context.Doctors
                .Any(d => d.Id == doctorId && d.Status == StatusType.Active && !d.IsDeleted);
            
            if (!doctorExists)
            {
                _logger.LogWarning($"Attempted to select non-existent or inactive doctor with ID: {doctorId}");
                return RedirectToAction("Doctor");
            }

            // Store in session
            HttpContext.Session.SetInt32("SelectedDoctorId", doctorId);
            
            // Redirect to next page
            return Redirect(redirectUrl ?? "/Patient/Appointments/Book/DateTime");
        }
    
        // GET: Patient/Appointments/Book/DateTime
        public async Task<IActionResult> SelectDateTime()
        {
            // Get the selected treatment and doctor IDs from session
            var selectedTreatmentId = HttpContext.Session.GetInt32("SelectedTreatmentId");
            var selectedDoctorId = HttpContext.Session.GetInt32("SelectedDoctorId");
            
            if (selectedTreatmentId == null || selectedDoctorId == null)
            {
                // If missing required selections, redirect appropriately
                if (selectedTreatmentId == null)
                {
                    return RedirectToAction("Book");
                }
                
                if (selectedDoctorId == null)
                {
                    return RedirectToAction("Doctor");
                }
            }
            
            // Get treatment details
            var treatment = await _context.TreatmentTypes
                .Where(t => t.Id == selectedTreatmentId)
                .FirstOrDefaultAsync();
                    
            if (treatment != null)
            {
                ViewData["TreatmentName"] = treatment.Name;
                ViewData["TreatmentDuration"] = $"{treatment.Duration} min";
                ViewData["TreatmentDurationMinutes"] = treatment.Duration; // For slot calculations
            }
            
            // Get doctor details
            var doctor = await _context.Doctors
                .Where(d => d.Id == selectedDoctorId)
                .FirstOrDefaultAsync();
                    
            if (doctor != null)
            {
                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                ViewData["DoctorSpecialization"] = doctor.Specialty;
            }
            
            // Get available time slots for the selected doctor
            // For the next 30 days - Use UTC dates
            var today = DateTime.UtcNow.Date;
            var endDate = today.AddDays(30);
            
            // Calculate how many consecutive time slots we need based on treatment duration
            var treatmentDuration = treatment?.Duration ?? 60; // Default to 60 minutes if not found
            
            // Get available slots with consecutive slot check
            var availableSlots = await GetAvailableSlotsAsync(
                selectedDoctorId.Value, 
                treatmentDuration, 
                today, 
                endDate
            );
            
            // Convert to JSON for JavaScript
            ViewData["TimeSlotData"] = System.Text.Json.JsonSerializer.Serialize(availableSlots);
            
            return View("SelectDateTime");
        }

        public async Task<Dictionary<string, List<TimeSlotViewModel>>> GetAvailableSlotsAsync(int doctorId, int treatmentDurationMinutes, DateTime startDate, DateTime endDate)
        {
            // Get current time for filtering today's slots
            var currentTime = DateTime.UtcNow;
            var oneHourFromNow = currentTime.AddHours(1);
            
            _logger.LogInformation($"Current time: {currentTime}");
            _logger.LogInformation($"One hour from now: {oneHourFromNow}");
            
            // Get all available slots for the doctor
            var allSlots = await _context.TimeSlots
                .Where(ts => ts.DoctorId == doctorId && 
                        ts.StartTime >= startDate && 
                        ts.StartTime <= endDate && 
                        !ts.IsBooked)
                .OrderBy(ts => ts.StartTime)
                .ToListAsync();
            
            _logger.LogInformation($"Total available slots before filtering: {allSlots.Count}");
            
            // Filter out slots for today that are less than 1 hour from now
            var filteredSlots = allSlots.Where(slot => 
                slot.StartTime.Date != currentTime.Date || // Not today
                slot.StartTime >= oneHourFromNow)         // Or at least 1 hour from now
                .ToList();
            
            _logger.LogInformation($"Slots after time filtering: {filteredSlots.Count}");
            
            // Calculate how many consecutive slots we need
            int requiredSlots = (int)Math.Ceiling(treatmentDurationMinutes / 60.0);
            
            // Group slots by date
            var slotsByDate = filteredSlots.GroupBy(s => s.StartTime.Date);
            
            // Dictionary to hold our results, grouped by date
            var availableTimeSlots = new Dictionary<string, List<TimeSlotViewModel>>();
            
            foreach (var dateGroup in slotsByDate)
            {
                var date = dateGroup.Key;
                var dateString = date.ToString("yyyy-MM-dd");
                var slotsForDate = new List<TimeSlotViewModel>();
                
                // Get all slots for this date
                var daySlots = dateGroup.OrderBy(s => s.StartTime).ToList();
                
                // Loop through slots to find consecutive available slots
                for (int i = 0; i <= daySlots.Count - requiredSlots; i++)
                {
                    bool consecutiveSlotsAvailable = true;
                    
                    // Check if we have enough consecutive slots
                    for (int j = 0; j < requiredSlots; j++)
                    {
                        // If this isn't the first slot, check if it's consecutive with the previous
                        if (j > 0)
                        {
                            var previousSlot = daySlots[i + j - 1];
                            var currentSlot = daySlots[i + j];
                            
                            // Check if slots are consecutive (end time of previous equals start time of current)
                            if (previousSlot.EndTime != currentSlot.StartTime)
                            {
                                consecutiveSlotsAvailable = false;
                                break;
                            }
                        }
                        
                        // Also check that the slot isn't already booked
                        if (daySlots[i + j].IsBooked)
                        {
                            consecutiveSlotsAvailable = false;
                            break;
                        }
                    }
                    
                    // If we found enough consecutive slots, add the first one to our results
                    if (consecutiveSlotsAvailable)
                    {
                        var firstSlot = daySlots[i];
                        var viewModel = new TimeSlotViewModel
                        {
                            Id = firstSlot.Id,
                            StartTime = firstSlot.StartTime,
                            EndTime = daySlots[i + requiredSlots - 1].EndTime, // End time of the last slot
                            FormattedStartTime = firstSlot.StartTime.ToString("h:mm tt"),
                            Period = GetTimePeriod(firstSlot.StartTime),
                            RequiredSlots = requiredSlots,
                            // Store IDs of all slots needed for this appointment
                            ConsecutiveSlotIds = Enumerable.Range(i, requiredSlots)
                                                .Select(idx => daySlots[idx].Id)
                                                .ToList()
                        };
                        
                        slotsForDate.Add(viewModel);
                    }
                }
                
                // Add slots for this date to our dictionary
                if (slotsForDate.Any())
                {
                    availableTimeSlots[dateString] = slotsForDate;
                    _logger.LogInformation($"Added {slotsForDate.Count} slots for date {dateString}");
                }
            }
            
            return availableTimeSlots;
        }

        private string GetTimePeriod(DateTime time)
        {
            var hour = time.Hour;
            if (hour >= 5 && hour < 12)
            {
                return "Morning";
            }
            else if (hour >= 12 && hour < 17)
            {
                return "Afternoon";
            }
            else
            {
                return "Evening";
            }
        }

        // Add this method to your AppointmentsController
        private async Task EnsureTimeSlots(int doctorId)
        {
            // Check if there are existing time slots for this doctor
            var hasTimeSlots = await _context.TimeSlots
                .AnyAsync(ts => ts.DoctorId == doctorId);
            
            if (!hasTimeSlots)
            {
                // Generate time slots for the next 30 days
                var today = DateTime.UtcNow.Date;
                var timeSlots = new List<TimeSlot>();
                
                for (int day = 0; day < 30; day++)
                {
                    var date = today.AddDays(day);
                    
                    // Skip weekends (Saturday and Sunday)
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                        continue;
                        
                    // Morning slots: 9:00 AM to 12:00 PM
                    for (int hour = 9; hour < 12; hour++)
                    {
                        // Every 30 minutes
                        for (int minute = 0; minute < 60; minute += 30)
                        {
                            // Randomly skip some slots to simulate a realistic schedule
                            if (new Random().Next(100) < 60) // 60% chance of having a slot
                            {
                                var startTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);
                                var endTime = startTime.AddMinutes(30);
                                
                                timeSlots.Add(new TimeSlot
                                {
                                    DoctorId = doctorId,
                                    StartTime = startTime,
                                    EndTime = endTime,
                                    IsBooked = false
                                });
                            }
                        }
                    }
                    
                    // Afternoon slots: 1:00 PM to 5:00 PM
                    for (int hour = 13; hour < 17; hour++)
                    {
                        for (int minute = 0; minute < 60; minute += 30)
                        {
                            if (new Random().Next(100) < 60)
                            {
                                var startTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);
                                var endTime = startTime.AddMinutes(30);
                                
                                timeSlots.Add(new TimeSlot
                                {
                                    DoctorId = doctorId,
                                    StartTime = startTime,
                                    EndTime = endTime,
                                    IsBooked = false
                                });
                            }
                        }
                    }
                    
                    // Evening slots: 5:00 PM to 8:00 PM
                    for (int hour = 17; hour < 20; hour++)
                    {
                        for (int minute = 0; minute < 60; minute += 30)
                        {
                            if (new Random().Next(100) < 60)
                            {
                                var startTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);
                                var endTime = startTime.AddMinutes(30);
                                
                                timeSlots.Add(new TimeSlot
                                {
                                    DoctorId = doctorId,
                                    StartTime = startTime,
                                    EndTime = endTime,
                                    IsBooked = false
                                });
                            }
                        }
                    }
                }
                
                // Add time slots to the database
                await _context.TimeSlots.AddRangeAsync(timeSlots);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Created {timeSlots.Count} time slots for doctor ID {doctorId}");
            }
        }
        
        // POST: Patient/Appointments/SaveDateTimeSelection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveDateTimeSelection(DateTime appointmentDate, string appointmentTime, string slotIds, string redirectUrl)
        {
            // Log all session values for debugging
            _logger.LogInformation($"TreatmentId in session: {HttpContext.Session.GetInt32("SelectedTreatmentId")}");
            _logger.LogInformation($"DoctorId in session: {HttpContext.Session.GetInt32("SelectedDoctorId")}");
            
            // Basic validation for date and time
            if (appointmentDate < DateTime.UtcNow.Date)
            {
                _logger.LogWarning($"Attempted to select past date: {appointmentDate}");
                return RedirectToAction("SelectDateTime");
            }

            // Convert input date to UTC if needed
            if (appointmentDate.Kind != DateTimeKind.Utc)
            {
                appointmentDate = DateTime.SpecifyKind(appointmentDate, DateTimeKind.Utc);
            }

            // Store in session
            HttpContext.Session.SetString("AppointmentDate", appointmentDate.ToString("yyyy-MM-dd"));
            HttpContext.Session.SetString("AppointmentTime", appointmentTime);
            
            // Store the consecutive slot IDs for the appointment
            if (!string.IsNullOrEmpty(slotIds))
            {
                HttpContext.Session.SetString("SlotIds", slotIds);
                _logger.LogInformation($"Stored slot IDs: {slotIds}");
            }
            
            // Log all stored values before redirect
            _logger.LogInformation($"Stored date: {appointmentDate.ToString("yyyy-MM-dd")}, time: {appointmentTime}");
            
            // Redirect to next page
            return RedirectToAction("Confirm");
        }
        
        // GET: Patient/Appointments/Book/Confirm
        public async Task<IActionResult> Confirm()
        {
            // Log the start of method
            _logger.LogInformation("Starting Confirm method");
            
            // Get all booking details from session
            var treatmentId = HttpContext.Session.GetInt32("SelectedTreatmentId");
            var doctorId = HttpContext.Session.GetInt32("SelectedDoctorId");
            var appointmentDate = HttpContext.Session.GetString("AppointmentDate");
            var appointmentTime = HttpContext.Session.GetString("AppointmentTime");
            
            // Log session values
            _logger.LogInformation($"Session values - TreatmentId: {treatmentId}, DoctorId: {doctorId}, Date: {appointmentDate}, Time: {appointmentTime}");
            
            // Check for data in the request (could be passed as query parameters)
            if (treatmentId == null && Request.Query.ContainsKey("treatmentId"))
            {
                if (int.TryParse(Request.Query["treatmentId"], out int queryTreatmentId))
                {
                    treatmentId = queryTreatmentId;
                    HttpContext.Session.SetInt32("SelectedTreatmentId", queryTreatmentId);
                }
            }
            
            if (doctorId == null && Request.Query.ContainsKey("doctorId"))
            {
                if (int.TryParse(Request.Query["doctorId"], out int queryDoctorId))
                {
                    doctorId = queryDoctorId;
                    HttpContext.Session.SetInt32("SelectedDoctorId", queryDoctorId);
                }
            }
            
            // If missing any required selections, redirect appropriately
            if (treatmentId == null)
            {
                return RedirectToAction("Book");
            }
            
            if (doctorId == null)
            {
                return RedirectToAction("Doctor");
            }
            
            if (string.IsNullOrEmpty(appointmentDate) || string.IsNullOrEmpty(appointmentTime))
            {
                return RedirectToAction("SelectDateTime");
            }
            
            // Prepare view model
            var bookingModel = new AppointmentBookingViewModel();
            
            // Get treatment details
            var treatment = await _context.TreatmentTypes
                .Where(t => t.Id == treatmentId)
                .FirstOrDefaultAsync();
                
            if (treatment != null)
            {
                bookingModel.TreatmentId = treatment.Id;
                bookingModel.TreatmentName = treatment.Name;
                bookingModel.TreatmentDuration = $"{treatment.Duration} min";
                bookingModel.TreatmentPrice = $"RM {treatment.Price}";
                
                ViewData["TreatmentName"] = treatment.Name;
                ViewData["TreatmentDuration"] = $"{treatment.Duration} min";
                ViewData["TreatmentPrice"] = $"RM {treatment.Price}";
            }
            
            // Get doctor details
            var doctor = await _context.Doctors
                .Where(d => d.Id == doctorId)
                .FirstOrDefaultAsync();
                
            if (doctor != null)
            {
                bookingModel.DoctorId = doctor.Id;
                bookingModel.DoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}";
                bookingModel.DoctorSpecialization = doctor.Specialty;
                
                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                ViewData["DoctorSpecialization"] =doctor.Specialty;
            }
            
            // Set appointment date and time
            if (DateTime.TryParse(appointmentDate, out DateTime parsedDate))
            {
                bookingModel.AppointmentDate = parsedDate;
                ViewData["AppointmentDate"] = parsedDate.ToString("dddd, MMMM d, yyyy");
            }
            
            bookingModel.AppointmentTime = appointmentTime;
            
            // Format the time for display (convert 24h to 12h format)
            if (TimeSpan.TryParse(appointmentTime, out TimeSpan time))
            {
                bool isPM = time.Hours >= 12;
                int hour12 = time.Hours % 12;
                if (hour12 == 0) hour12 = 12;
                string formattedTime = $"{hour12}:{time.Minutes:D2} {(isPM ? "PM" : "AM")}";
                ViewData["AppointmentTime"] = formattedTime;
            }
            
            return View(bookingModel);
        }
        
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitBooking(AppointmentBookingViewModel model)
        {
            // Add extensive debug logging
            _logger.LogInformation($"SubmitBooking started with TreatmentId={model.TreatmentId}, DoctorId={model.DoctorId}");
            _logger.LogInformation($"AppointmentDate={model.AppointmentDate}, AppointmentTime={model.AppointmentTime}");
            
            // We need to manually validate only the required fields and ignore validation
            // for the display fields that aren't being submitted by the form
            ModelState.Clear(); // Clear all validation since we'll do it manually
            
            // Check only the required fields
            if (model.TreatmentId <= 0)
            {
                ModelState.AddModelError("TreatmentId", "Treatment is required");
            }
            
            if (model.DoctorId <= 0)
            {
                ModelState.AddModelError("DoctorId", "Doctor is required");
            }
            
            // Date is a value type, so it can't be null, but check if it's the default
            if (model.AppointmentDate == default)
            {
                ModelState.AddModelError("AppointmentDate", "Appointment date is required");
            }
            
            if (string.IsNullOrEmpty(model.AppointmentTime))
            {
                ModelState.AddModelError("AppointmentTime", "Appointment time is required");
            }
            
            // If we have validation errors, reload the page with the errors
            if (!ModelState.IsValid)
            {
                // Log validation errors
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogError($"Validation error for {state.Key}: {error.ErrorMessage}");
                    }
                }
                
                // Reload the data for the form
                await RepopulateViewModelData(model);
                
                return View("Confirm", model);
            }
            
            try
            {
                // Get current user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }
                
                // Get patient
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
                if (patient == null)
                {
                    ModelState.AddModelError("", "Patient record not found");
                    return View("Confirm", model);
                }
                
                // Get treatment for cost calculation
                var treatment = await _context.TreatmentTypes.FindAsync(model.TreatmentId);
                if (treatment == null)
                {
                    ModelState.AddModelError("", "Selected treatment not found");
                    return View("Confirm", model);
                }
                
                // Calculate deposit amount (30% of treatment cost)
                decimal depositAmount = treatment.Price * 0.3m;
                
                // Create the appointment with a temporary status
                var appointment = new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = model.DoctorId,
                    TreatmentTypeId = model.TreatmentId,
                    AppointmentDate = DateTime.SpecifyKind(model.AppointmentDate, DateTimeKind.Utc),
                    AppointmentTime = TimeSpan.Parse(model.AppointmentTime),
                    Duration = treatment.Duration,
                    Notes = model.Notes ?? "",
                    Status = "Pending-Payment", // New status before payment
                    CreatedAt = DateTime.UtcNow,
                    PaymentStatus = PaymentStatus.Pending,
                    DepositAmount = depositAmount,
                    TotalAmount = treatment.Price
                };
                
                // Save the appointment to get an ID
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                
                // Construct URLs
                string successUrl = Url.Action(
                    "BookingSuccess", 
                    "Appointments", 
                    new { area = "Patient", id = appointment.Id }, 
                    Request.Scheme
                );
                
                string cancelUrl = Url.Action(
                    "CancelBooking", 
                    "Appointments", 
                    new { area = "Patient" }, 
                    Request.Scheme
                );
                
                // Add failure URL
                string failureUrl = Url.Action(
                    "BookingFailure", 
                    "Appointments", 
                    new { area = "Patient", id = appointment.Id }, 
                    Request.Scheme
                );
                
                // Create Stripe checkout session with failure URL
                var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                    appointment.Id, 
                    depositAmount, 
                    successUrl, 
                    cancelUrl,
                    failureUrl
                );
                
                // Redirect to Stripe checkout
                return Redirect(checkoutUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitBooking");
                ModelState.AddModelError("", "An error occurred while processing your booking");
                return View("Confirm", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CancelBooking()
        {
            try 
            {
                // Get current user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                // Find the patient associated with the user
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
                if (patient == null)
                {
                    return RedirectToAction("Index");
                }

                // Clear all session variables related to booking process
                HttpContext.Session.Clear();


                // Optional: Remove any pending appointments in "Pending-Payment" status
                // var pendingAppointments = await _context.Appointments
                //     .Where(a => a.PatientId == patient.Id && a.Status == "Pending-Payment")
                //     .ToListAsync();

                // if (pendingAppointments.Any())
                // {
                //     _context.Appointments.RemoveRange(pendingAppointments);
                //     await _context.SaveChangesAsync();
                // }

                // Log the cancellation
                _logger.LogInformation("Booking process cancelled and session state cleared");

                // Redirect to the main appointments page
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log any errors
                _logger.LogError(ex, "Error in CancelBooking method");
                return RedirectToAction("Index");
            }
        }

        // Helper method to reload data for the view model
        private async Task RepopulateViewModelData(AppointmentBookingViewModel model)
        {
            try
            {
                // Get treatment details
                if (model.TreatmentId > 0)
                {
                    var treatment = await _context.TreatmentTypes.FindAsync(model.TreatmentId);
                    if (treatment != null)
                    {
                        model.TreatmentName = treatment.Name;
                        model.TreatmentDuration = $"{treatment.Duration} min";
                        model.TreatmentPrice = $"RM {treatment.Price}";
                        
                        _logger.LogInformation($"Repopulated treatment: {model.TreatmentName}, {model.TreatmentDuration}, {model.TreatmentPrice}");
                    }
                }
                
                // Get doctor details
                if (model.DoctorId > 0)
                {
                    var doctor = await _context.Doctors.FindAsync(model.DoctorId);
                    if (doctor != null)
                    {
                        model.DoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}";
                        model.DoctorSpecialization = doctor.Specialty;
                        
                        _logger.LogInformation($"Repopulated doctor: {model.DoctorName}, {model.DoctorSpecialization}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error repopulating view model data");
            }
        }

        
        [HttpGet("BookingFailure/{id}")]
        public async Task<IActionResult> BookingFailure(int id, string error = null)
        {
            _logger.LogInformation($"BookingFailure method called with Appointment ID: {id}");
            
            try 
            {
                // Fetch the appointment with all related details
                var appointment = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.TreatmentType)
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == id);
                
                if (appointment == null)
                {
                    _logger.LogWarning($"No appointment found with ID: {id}");
                    return RedirectToAction("Index");
                }
                
                // Verify the appointment belongs to the current user
                var user = await _userManager.GetUserAsync(User);
                if (user == null || appointment.Patient.UserID != user.Id)
                {
                    _logger.LogWarning($"Unauthorized access to appointment {id}");
                    return RedirectToAction("Index");
                }
                
                // Populate ViewData with appointment details
                ViewData["AppointmentId"] = appointment.Id;
                ViewData["AppointmentDate"] = appointment.AppointmentDate.ToString("dddd, MMMM d, yyyy");
                
                // Format time
                bool isPM = appointment.AppointmentTime.Hours >= 12;
                int hour12 = appointment.AppointmentTime.Hours % 12;
                if (hour12 == 0) hour12 = 12;
                string formattedTime = $"{hour12}:{appointment.AppointmentTime.Minutes:D2} {(isPM ? "PM" : "AM")}";
                ViewData["AppointmentTime"] = formattedTime;
                
                ViewData["DoctorName"] = $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}";
                ViewData["TreatmentName"] = appointment.TreatmentType.Name;
                ViewData["TreatmentCost"] = $"RM {appointment.TreatmentType.Price:0.00}";
                ViewData["DepositAmount"] = $"RM {(appointment.TreatmentType.Price * 0.3m):0.00}";
                
                // Set the error message or use a default
                ViewData["ErrorMessage"] = string.IsNullOrEmpty(error) 
                    ? "Your payment could not be processed." 
                    : error;
                
                _logger.LogInformation("Rendering BookingFailure view");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in BookingFailure method for appointment {id}");
                return RedirectToAction("Index");
            }
        }
                
        [HttpGet("BookingSuccess/{id}")]
        public async Task<IActionResult> BookingSuccess(int id)
        {
            _logger.LogInformation($"BookingSuccess method called with Appointment ID: {id}");
            
            try 
            {
                // Fetch the appointment with all related details
                var appointment = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.TreatmentType)
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == id);
                
                if (appointment == null)
                {
                    _logger.LogWarning($"No appointment found with ID: {id}");
                    return RedirectToAction("Index");
                }
                
                // Verify the appointment belongs to the current user
                var user = await _userManager.GetUserAsync(User);
                if (user == null || appointment.Patient.UserID != user.Id)
                {
                    _logger.LogWarning($"Unauthorized access to appointment {id}");
                    return RedirectToAction("Index");
                }
                
                // Populate ViewData with appointment details
                ViewData["AppointmentId"] = appointment.Id;
                ViewData["AppointmentDate"] = appointment.AppointmentDate.ToString("dddd, MMMM d, yyyy");
                
                // Format time
                bool isPM = appointment.AppointmentTime.Hours >= 12;
                int hour12 = appointment.AppointmentTime.Hours % 12;
                if (hour12 == 0) hour12 = 12;
                string formattedTime = $"{hour12}:{appointment.AppointmentTime.Minutes:D2} {(isPM ? "PM" : "AM")}";
                ViewData["AppointmentTime"] = formattedTime;
                
                ViewData["DoctorName"] = $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}";
                ViewData["TreatmentName"] = appointment.TreatmentType.Name;
                ViewData["TreatmentCost"] = $"RM {appointment.TreatmentType.Price:0.00}";
                ViewData["DepositAmount"] = $"RM {(appointment.TreatmentType.Price * 0.3m):0.00}";
                
                _logger.LogInformation("Rendering BookingSuccess view");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in BookingSuccess method for appointment {id}");
                return RedirectToAction("Index");
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.TimeSlots)
                .Include(a => a.Doctor)
                .Include(a => a.TreatmentType)
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .Include(a => a.Payments)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (appointment == null)
            {
                return NotFound();
            }
            
            // Check if appointment belongs to current user
            var user = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
            
            if (patient == null || appointment.PatientId != patient.Id)
            {
                return Forbid();
            }
            
            // Check if appointment can be cancelled
            var today = DateTime.UtcNow.Date;
            var canCancel = appointment.Status != "Cancelled" && appointment.Status != "Completed" && 
                        (appointment.AppointmentDate > today || 
                        (appointment.AppointmentDate == today && appointment.AppointmentTime > DateTime.UtcNow.TimeOfDay));
                        
            if (!canCancel)
            {
                TempData["ErrorMessage"] = "This appointment cannot be cancelled.";
                return RedirectToAction(nameof(Index));
            }
            
            // Store the status before update for email notification logic
            var previousStatus = appointment.Status;
            
            // Cancel appointment
            appointment.Status = "Cancelled";
            appointment.UpdatedAt = DateTime.UtcNow;
            
            // Release associated time slots
            var associatedTimeSlots = await _context.TimeSlots
                .Where(ts => ts.AppointmentId == appointment.Id)
                .ToListAsync();
            
            foreach (var timeSlot in associatedTimeSlots)
            {
                timeSlot.IsBooked = false;
                timeSlot.AppointmentId = null;
            }
            
            await _context.SaveChangesAsync();
            
            // Format appointment date and time for notifications
            string formattedDate = appointment.AppointmentDate.ToString("MMMM d, yyyy");
            
            // Format time
            bool isPM = appointment.AppointmentTime.Hours >= 12;
            int hour12 = appointment.AppointmentTime.Hours % 12;
            if (hour12 == 0) hour12 = 12;
            string formattedTime = $"{hour12}:{appointment.AppointmentTime.Minutes:D2} {(isPM ? "PM" : "AM")}";
            
            // Determine refund details
            string refundMessage = "";

            // If appointment is in Pending-Payment status, just cancel
            if (appointment.PaymentStatus == PaymentStatus.Pending)
            {
                refundMessage = "This appointment was in pending status and has been cancelled.";
            }
            else
            {
                // Existing refund logic for paid appointments
                bool isEligibleForRefund = false;
                bool refundProcessed = false;
                decimal refundAmount = 0;

                try 
                {
                    // Check if appointment is more than 24 hours away
                    DateTime appointmentDateTime = appointment.AppointmentDate.Date + appointment.AppointmentTime;
                    isEligibleForRefund = (appointmentDateTime - DateTime.UtcNow).TotalHours > 24;
                    
                    // If eligible, attempt to process refund
                    if (isEligibleForRefund)
                    {
                        // Find the deposit payment
                        var depositPayment = appointment.Payments
                            .FirstOrDefault(p => p.PaymentType == PaymentType.Deposit && p.Status == "succeeded");
                        
                        if (depositPayment != null)
                        {
                            refundAmount = depositPayment.Amount;
                            
                            // Attempt to process refund
                            refundProcessed = await _paymentService.ProcessRefundAsync(appointment.Id);
                        }
                    }

                    refundMessage = DetermineRefundMessage(isEligibleForRefund, refundProcessed, refundAmount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing refund");
                    refundMessage = "An error occurred while processing the cancellation.";
                }
            }
            
            // Create in-app notification for appointment cancellation
            var notification = new UserNotification
            {
                UserId = user.Id,
                NotificationType = "Appointment_Cancelled",
                Title = "Appointment Cancelled",
                Message = $"Your {appointment.TreatmentType.Name} appointment scheduled for {formattedDate} at {formattedTime} has been cancelled. {refundMessage}",
                RelatedEntityId = appointment.Id,
                ActionController = "Appointments",
                ActionName = "Details",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.UserNotifications.Add(notification);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Created in-app notification for cancelled appointment ID: {appointment.Id}");
            
            // Existing email notification logic remains unchanged
            if (previousStatus == "Scheduled" || previousStatus == "Confirmed")
            {
                try
                {
                    // Get the user's notification preferences
                    var preferences = await _context.UserNotificationPreferences
                        .FirstOrDefaultAsync(p => p.UserId == user.Id);
                        
                    // If no preferences found, create default ones with email notifications enabled
                    if (preferences == null)
                    {
                        preferences = new UserNotificationPreferences
                        {
                            UserId = user.Id,
                            EmailAppointmentReminders = true,
                            EmailNewAppointments = true,
                            EmailAppointmentChanges = true,
                            EmailPromotions = true,
                            LastUpdated = DateTime.UtcNow
                        };
                        
                        _context.UserNotificationPreferences.Add(preferences);
                        await _context.SaveChangesAsync();
                    }
                    
                    // Only send cancellation email if user has enabled the preference
                    if (preferences.EmailAppointmentChanges)
                    {
                        // Create appointment details for email
                        var appointmentDetails = new AppointmentDetailViewModel
                        {
                            Id = appointment.Id,
                            TreatmentName = appointment.TreatmentType.Name,
                            DoctorName = $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}",
                            DoctorSpecialization = appointment.Doctor.Specialty,
                            AppointmentDate = appointment.AppointmentDate,
                            AppointmentTime = appointment.AppointmentTime,
                            Status = "Cancelled",
                            CreatedOn = appointment.CreatedAt,
                            TreatmentDuration = appointment.TreatmentType.Duration,
                            TreatmentCost = appointment.TreatmentType.Price
                        };
                        
                        // Send cancellation email
                        await _emailService.SendAppointmentCancellationEmailAsync(
                            user.Email,
                            $"{patient.FirstName} {patient.LastName}",
                            appointmentDetails);
                        
                        // Update notification to track email sent
                        notification.EmailSent = true;
                        notification.EmailSentAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"Appointment cancellation email sent to {user.Email}");
                    }
                    else
                    {
                        _logger.LogInformation($"Skipped sending cancellation email to {user.Email} based on user preferences");
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the cancellation if email fails
                    _logger.LogError(ex, "Error handling appointment cancellation notification");
                }
            }
            
            TempData["SuccessMessage"] = "Your appointment has been cancelled.";
            return RedirectToAction(nameof(Index));
        }

        // Helper method to determine refund message
        private string DetermineRefundMessage(bool isEligibleForRefund, bool refundProcessed, decimal refundAmount)
        {
            if (!isEligibleForRefund)
            {
                return "This appointment is not eligible for a refund due to being within 24 hours of the scheduled time.";
            }

            if (refundProcessed)
            {
                return $"A full deposit refund of RM {refundAmount:0.00} has been processed and will be credited back to your original payment method within 5-10 business days.";
            }

            return "You are eligible for a refund, but there was an issue processing it. Please contact our support team for assistance.";
        }
        
        // GET: Patient/Appointments/Details/{id}
        // Additional action for processing remaining payment will be needed
        public async Task<IActionResult> Details(int id)
        {
            var today = DateTime.UtcNow.Date;
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.TreatmentType)
                .Include(a => a.Payments) // Include payments related to this appointment
                .FirstOrDefaultAsync(a => a.Id == id);
                    
            if (appointment == null)
            {
                return NotFound();
            }
            
            // Check if appointment belongs to current user
            var user = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
            
            if (patient == null || appointment.PatientId != patient.Id)
            {
                return Forbid();
            }
            
            // Find payment dates and calculate amounts
            DateTime? depositPaymentDate = null;
            DateTime? fullPaymentDate = null;
            decimal amountPaid = 0;
            
            if (appointment.Payments != null && appointment.Payments.Any())
            {
                foreach (var payment in appointment.Payments.Where(p => p.Status == "succeeded"))
                {
                    // Add to the total paid amount
                    amountPaid += payment.Amount;
                    
                    // Track payment dates based on type
                    if (payment.PaymentType == PaymentType.Deposit)
                    {
                        depositPaymentDate = payment.CreatedAt;
                    }
                    else if (payment.PaymentType == PaymentType.FullPayment)
                    {
                        fullPaymentDate = payment.CreatedAt;
                    }
                }
            }
            
            // Calculate remaining amount
            decimal remainingAmount = appointment.TotalAmount - amountPaid;
            
            // Create view model
            var viewModel = new AppointmentDetailViewModel
            {
                Id = appointment.Id,
                TreatmentName = appointment.TreatmentType.Name,
                DoctorName = $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}",
                DoctorSpecialization = appointment.Doctor.Specialty,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                Status = appointment.Status,
                Notes = appointment.Notes,
                CreatedOn = appointment.CreatedAt,
                TreatmentCost = appointment.TreatmentType.Price,
                TreatmentDuration = appointment.TreatmentType.Duration,
                CanCancel = appointment.Status != "Cancelled" && appointment.Status != "Completed" && 
                        (appointment.AppointmentDate > today || 
                        (appointment.AppointmentDate == today && appointment.AppointmentTime > DateTime.UtcNow.TimeOfDay)),
                // Payment related properties
                PaymentStatus = appointment.PaymentStatus,
                DepositAmount = appointment.DepositAmount,
                AmountPaid = amountPaid,
                RemainingAmount = remainingAmount,
                LastPaymentDate = depositPaymentDate,
                FullPaymentDate = fullPaymentDate
            };
            
            return View(viewModel);
        }

        public async Task BookTimeSlotsAsync(int appointmentId, List<int> slotIds)
        {
            // Get all the slots we need to book
            var slots = await _context.TimeSlots
                .Where(ts => slotIds.Contains(ts.Id))
                .ToListAsync();
            
            // Update each slot
            foreach (var slot in slots)
            {
                slot.IsBooked = true;
                slot.AppointmentId = appointmentId;
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task<List<int>> FindConsecutiveSlotIdsAsync(int doctorId, DateTime appointmentDate, TimeSpan appointmentTime, int durationMinutes)
        {
            // Find the starting slot
            var startSlot = await _context.TimeSlots
                .FirstOrDefaultAsync(ts => 
                    ts.DoctorId == doctorId && 
                    ts.StartTime.Date == appointmentDate.Date && 
                    ts.StartTime.TimeOfDay == appointmentTime && 
                    !ts.IsBooked);
            
            if (startSlot == null)
            {
                throw new Exception("Starting time slot not available.");
            }
            
            // How many consecutive slots we need
            int requiredSlots = (int)Math.Ceiling(durationMinutes / 60.0);
            
            // All consecutive slots
            var slotIds = new List<int> { startSlot.Id };
            
            // If we need more than one slot
            if (requiredSlots > 1)
            {
                // Get the next slots after the start slot
                var nextStartTime = startSlot.EndTime;
                
                // Find the following slots
                for (int i = 1; i < requiredSlots; i++)
                {
                    var nextSlot = await _context.TimeSlots
                        .FirstOrDefaultAsync(ts => 
                            ts.DoctorId == doctorId && 
                            ts.StartTime == nextStartTime && 
                            !ts.IsBooked);
                    
                    if (nextSlot == null)
                    {
                        throw new Exception($"Required consecutive slot at {nextStartTime} not available.");
                    }
                    
                    slotIds.Add(nextSlot.Id);
                    nextStartTime = nextSlot.EndTime;
                }
            }
            
            return slotIds;
        }

       // POST: Patient/Appointments/UpdateNotificationPreferences
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNotificationPreferences(UserNotificationPreferences model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }
                
                // Get existing preferences
                var preferences = await _context.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);
                    
                // If no preferences found, create new ones
                if (preferences == null)
                {
                    preferences = new UserNotificationPreferences
                    {
                        UserId = user.Id,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.UserNotificationPreferences.Add(preferences);
                }
                
                // Update email preferences
                preferences.EmailAppointmentReminders = model.EmailAppointmentReminders;
                preferences.EmailNewAppointments = model.EmailNewAppointments;
                preferences.EmailAppointmentChanges = model.EmailAppointmentChanges;
                preferences.EmailPromotions = model.EmailPromotions;
                preferences.LastUpdated = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Notification preferences updated successfully.";
                
                // Redirect back to the settings page or wherever appropriate
                return RedirectToAction("NotificationSettings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification preferences");
                TempData["ErrorMessage"] = "Failed to update notification preferences.";
                return RedirectToAction("NotificationSettings");
            }
        }

        // GET: Patient/Appointments/NotificationSettings
        public async Task<IActionResult> NotificationSettings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            // Get user's notification preferences
            var preferences = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
                
            // If no preferences exist, create default ones
            if (preferences == null)
            {
                preferences = new UserNotificationPreferences
                {
                    UserId = user.Id,
                    EmailAppointmentReminders = true,
                    EmailNewAppointments = true,
                    EmailAppointmentChanges = true,
                    EmailPromotions = true,
                    LastUpdated = DateTime.UtcNow
                };
                
                _context.UserNotificationPreferences.Add(preferences);
                await _context.SaveChangesAsync();
            }
            
            return View(preferences);
        }

        [HttpGet]
public async Task<IActionResult> CheckSlotAvailability(int id)
{
    try 
    {
        // Find the appointment
        var appointment = await _context.Appointments
            .Include(a => a.TimeSlots)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (appointment == null)
        {
            return Json(new { isAvailable = false });
        }

        // Check if all associated time slots are still available
        var slotIds = appointment.TimeSlots.Select(ts => ts.Id).ToList();
        
        var availableSlots = await _context.TimeSlots
            .Where(ts => slotIds.Contains(ts.Id))
            .ToListAsync();
        
        // Check if all slots are still not booked
        bool allSlotsAvailable = availableSlots.All(slot => !slot.IsBooked);
        
        return Json(new { isAvailable = allSlotsAvailable });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error checking slot availability");
        return Json(new { isAvailable = false });
    }
}

    [HttpGet]
    public async Task<IActionResult> ProceedToPayment(int id)
    {
        try 
        {
            // Find the appointment
            var appointment = await _context.Appointments
                .Include(a => a.TreatmentType)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("Index");
            }

            // Verify the appointment belongs to the current user
            var user = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
            
            if (patient == null || appointment.PatientId != patient.Id)
            {
                return Forbid();
            }

            // Calculate deposit amount (30% of treatment cost)
            decimal depositAmount = appointment.TreatmentType.Price * 0.3m;
            
            // Construct URLs
            string successUrl = Url.Action(
                "BookingSuccess", 
                "Appointments", 
                new { area = "Patient", id = appointment.Id }, 
                Request.Scheme
            );
            
            string cancelUrl = Url.Action(
                "Details", 
                "Appointments", 
                new { area = "Patient", id = appointment.Id }, 
                Request.Scheme
            );
            
            // Add failure URL
            string failureUrl = Url.Action(
                "BookingFailure", 
                "Appointments", 
                new { area = "Patient", id = appointment.Id }, 
                Request.Scheme
            );
            
            // Create Stripe checkout session with failure URL
            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                appointment.Id, 
                depositAmount, 
                successUrl, 
                cancelUrl,
                failureUrl
            );
            
            // Redirect to Stripe checkout
            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proceeding to payment");
            TempData["ErrorMessage"] = "An error occurred while processing your payment.";
            return RedirectToAction("Index");
        }
    }


    [HttpGet]
    public async Task<IActionResult> ProcessRemainingPayment(int id)
    {
        try 
        {
            // Find the appointment
            var appointment = await _context.Appointments
                .Include(a => a.TreatmentType)
                .Include(a => a.Payments)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("Index");
            }

            // Verify the appointment belongs to the current user
            var user = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
            
            if (patient == null || appointment.PatientId != patient.Id)
            {
                return Forbid();
            }

            // Calculate the remaining amount to pay
            decimal amountPaid = appointment.Payments
                .Where(p => p.Status == "succeeded")
                .Sum(p => p.Amount);
            
            decimal remainingAmount = appointment.TotalAmount - amountPaid;
            
            if (remainingAmount <= 0)
            {
                TempData["InfoMessage"] = "This appointment has already been fully paid.";
                return RedirectToAction("Details", new { id = appointment.Id });
            }

            // Construct URLs
            string successUrl = Url.Action(
                "PaymentSuccess", 
                "Appointments", 
                new { area = "Patient", id = appointment.Id, type = "remaining" }, 
                Request.Scheme
            );
            
            string cancelUrl = Url.Action(
                "Details", 
                "Appointments", 
                new { area = "Patient", id = appointment.Id }, 
                Request.Scheme
            );
            
            // Add failure URL
            string failureUrl = Url.Action(
                "BookingFailure", 
                "Appointments", 
                new { area = "Patient", id = appointment.Id, error = "Your remaining payment could not be processed." }, 
                Request.Scheme
            );
            
            // Create Stripe checkout session for the remaining amount with failure URL
            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                appointment.Id, 
                remainingAmount, 
                successUrl, 
                cancelUrl,
                failureUrl,
                PaymentType.FullPayment
            );
            
            // Redirect to Stripe checkout
            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing remaining payment");
            TempData["ErrorMessage"] = "An error occurred while processing your payment.";
            return RedirectToAction("Details", new { id = id });
        }
    }
        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int id, string type)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.TreatmentType)
                    .FirstOrDefaultAsync(a => a.Id == id);
                    
                if (appointment == null)
                {
                    return NotFound();
                }

                // Update appointment payment status to Paid if this was the remaining payment
                if (type == "remaining")
                {
                    appointment.Status = "Completed"; // Add this line to update the status to "Completed"
                    appointment.PaymentStatus = PaymentStatus.Paid;
                    appointment.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Payment successful! Your appointment is now fully paid.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Payment successful!";
                }
                
                return RedirectToAction("Details", new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in PaymentSuccess method for appointment {id}");
                TempData["ErrorMessage"] = "There was an issue processing your payment result.";
                return RedirectToAction("Details", new { id = id });
            }
        }
        }
    
}