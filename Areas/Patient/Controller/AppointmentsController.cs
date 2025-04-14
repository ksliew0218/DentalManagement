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
        private readonly IPaymentService _paymentService;
        private readonly string _applicationUrl;

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
            
            string applicationUrl = Environment.GetEnvironmentVariable("APPLICATION_URL") ?? "http://localhost:5001";
            _applicationUrl = applicationUrl.TrimEnd('/');
        }

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
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
                    if (patient != null)
                    {
                        var now = DateTime.UtcNow;
                        var today = now.Date;

                        var allAppointments = await _context.Appointments
                            .Include(a => a.Doctor)
                            .Include(a => a.TreatmentType)
                            .Where(a => a.PatientId == patient.Id)
                            .OrderBy(a => a.AppointmentDate)
                            .ThenBy(a => a.AppointmentTime)
                            .ToListAsync();

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
                                CanCancel = appointment.Status != "Cancelled" && 
                                        appointment.Status != "Completed" && 
                                        (appointment.AppointmentDate > today || 
                                        (appointment.AppointmentDate == today && 
                                            appointment.AppointmentTime > now.TimeOfDay))
                            };

                            if (appointment.Status == "Cancelled")
                            {
                                viewModel.CancelledAppointments.Add(appointmentViewModel);
                            }
                            else if (appointment.Status == "Completed")
                            {
                                viewModel.PastAppointments.Add(appointmentViewModel);
                            }
                            else
                            {
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
                _logger.LogError(ex, "Error fetching appointments");
            }
            
            return View("_MyAppointments", viewModel);
        }
        
        public async Task<IActionResult> Book()
        {
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
            
            var categories = new List<string> { "Cleaning", "Cosmetic", "Restorative", "Surgical", "Orthodontic" };
            
            var viewModel = new TreatmentSelectionViewModel
            {
                Categories = categories,
                Treatments = treatments
            };
            
            return View(viewModel);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveTreatmentSelection(int treatmentId, string redirectUrl)
        {
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
        
        public async Task<IActionResult> Doctor()
        {
            var selectedTreatmentId = HttpContext.Session.GetInt32("SelectedTreatmentId");
            
            if (selectedTreatmentId == null)
            {
                if (int.TryParse(Request.Query["treatmentId"], out int queryTreatmentId))
                {
                    selectedTreatmentId = queryTreatmentId;
                    HttpContext.Session.SetInt32("SelectedTreatmentId", queryTreatmentId);
                }
                else
                {
                    return RedirectToAction("Book");
                }
            }
            
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
            
            var treatment = await _context.TreatmentTypes
                .Where(t => t.Id == selectedTreatmentId)
                .FirstOrDefaultAsync();
                
            if (treatment != null)
            {
                ViewData["TreatmentName"] = treatment.Name;
                ViewData["TreatmentDuration"] = $"{treatment.Duration} min";
                ViewData["TreatmentPrice"] = $"RM {treatment.Price}";
                
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
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveDoctorSelection(int doctorId, string redirectUrl)
        {
            var doctorExists = _context.Doctors
                .Any(d => d.Id == doctorId && d.Status == StatusType.Active && !d.IsDeleted);
            
            if (!doctorExists)
            {
                _logger.LogWarning($"Attempted to select non-existent or inactive doctor with ID: {doctorId}");
                return RedirectToAction("Doctor");
            }

            HttpContext.Session.SetInt32("SelectedDoctorId", doctorId);
            
            return Redirect(redirectUrl ?? "/Patient/Appointments/Book/DateTime");
        }
    
        public async Task<IActionResult> SelectDateTime()
        {
            var selectedTreatmentId = HttpContext.Session.GetInt32("SelectedTreatmentId");
            var selectedDoctorId = HttpContext.Session.GetInt32("SelectedDoctorId");
            
            if (selectedTreatmentId == null || selectedDoctorId == null)
            {
                if (selectedTreatmentId == null)
                {
                    return RedirectToAction("Book");
                }
                
                if (selectedDoctorId == null)
                {
                    return RedirectToAction("Doctor");
                }
            }
            
            var treatment = await _context.TreatmentTypes
                .Where(t => t.Id == selectedTreatmentId)
                .FirstOrDefaultAsync();
                    
            if (treatment != null)
            {
                ViewData["TreatmentName"] = treatment.Name;
                ViewData["TreatmentDuration"] = $"{treatment.Duration} min";
                ViewData["TreatmentDurationMinutes"] = treatment.Duration; 
            }
            
            var doctor = await _context.Doctors
                .Where(d => d.Id == selectedDoctorId)
                .FirstOrDefaultAsync();
                    
            if (doctor != null)
            {
                ViewData["DoctorName"] = $"Dr. {doctor.FirstName} {doctor.LastName}";
                ViewData["DoctorSpecialization"] = doctor.Specialty;
            }
            
            var today = DateTime.UtcNow.Date;
            var endDate = today.AddDays(30);
            
            var treatmentDuration = treatment?.Duration ?? 60; 
            
            var availableSlots = await GetAvailableSlotsAsync(
                selectedDoctorId.Value, 
                treatmentDuration, 
                today, 
                endDate
            );
            
            ViewData["TimeSlotData"] = System.Text.Json.JsonSerializer.Serialize(availableSlots);
            
            return View("SelectDateTime");
        }

        public async Task<Dictionary<string, List<TimeSlotViewModel>>> GetAvailableSlotsAsync(int doctorId, int treatmentDurationMinutes, DateTime startDate, DateTime endDate)
        {
            var currentTime = DateTime.UtcNow;
            var oneHourFromNow = currentTime.AddHours(1);
            
            _logger.LogInformation($"Current time: {currentTime}");
            _logger.LogInformation($"One hour from now: {oneHourFromNow}");
            
            var allSlots = await _context.TimeSlots
                .Where(ts => ts.DoctorId == doctorId && 
                        ts.StartTime >= startDate && 
                        ts.StartTime <= endDate && 
                        !ts.IsBooked)
                .OrderBy(ts => ts.StartTime)
                .ToListAsync();
            
            _logger.LogInformation($"Total available slots before filtering: {allSlots.Count}");
            
            var filteredSlots = allSlots.Where(slot => 
                slot.StartTime.Date != currentTime.Date || 
                slot.StartTime >= oneHourFromNow)         
                .ToList();
            
            _logger.LogInformation($"Slots after time filtering: {filteredSlots.Count}");
            
            int requiredSlots = (int)Math.Ceiling(treatmentDurationMinutes / 60.0);
            
            var slotsByDate = filteredSlots.GroupBy(s => s.StartTime.Date);
            
            var availableTimeSlots = new Dictionary<string, List<TimeSlotViewModel>>();
            
            foreach (var dateGroup in slotsByDate)
            {
                var date = dateGroup.Key;
                var dateString = date.ToString("yyyy-MM-dd");
                var slotsForDate = new List<TimeSlotViewModel>();
                
                var daySlots = dateGroup.OrderBy(s => s.StartTime).ToList();
                
                for (int i = 0; i <= daySlots.Count - requiredSlots; i++)
                {
                    bool consecutiveSlotsAvailable = true;
                    
                    for (int j = 0; j < requiredSlots; j++)
                    {
                        if (j > 0)
                        {
                            var previousSlot = daySlots[i + j - 1];
                            var currentSlot = daySlots[i + j];
                            
                            if (previousSlot.EndTime != currentSlot.StartTime)
                            {
                                consecutiveSlotsAvailable = false;
                                break;
                            }
                        }
                        
                        if (daySlots[i + j].IsBooked)
                        {
                            consecutiveSlotsAvailable = false;
                            break;
                        }
                    }
                    
                    if (consecutiveSlotsAvailable)
                    {
                        var firstSlot = daySlots[i];
                        var viewModel = new TimeSlotViewModel
                        {
                            Id = firstSlot.Id,
                            StartTime = firstSlot.StartTime,
                            EndTime = daySlots[i + requiredSlots - 1].EndTime, 
                            FormattedStartTime = firstSlot.StartTime.ToString("h:mm tt"),
                            Period = GetTimePeriod(firstSlot.StartTime),
                            RequiredSlots = requiredSlots,
                            ConsecutiveSlotIds = Enumerable.Range(i, requiredSlots)
                                                .Select(idx => daySlots[idx].Id)
                                                .ToList()
                        };
                        
                        slotsForDate.Add(viewModel);
                    }
                }
                
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

        private async Task EnsureTimeSlots(int doctorId)
        {
            var hasTimeSlots = await _context.TimeSlots
                .AnyAsync(ts => ts.DoctorId == doctorId);
            
            if (!hasTimeSlots)
            {
                var today = DateTime.UtcNow.Date;
                var timeSlots = new List<TimeSlot>();
                
                for (int day = 0; day < 30; day++)
                {
                    var date = today.AddDays(day);
                    
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                        continue;
                        
                    for (int hour = 9; hour < 12; hour++)
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
                
                await _context.TimeSlots.AddRangeAsync(timeSlots);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Created {timeSlots.Count} time slots for doctor ID {doctorId}");
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveDateTimeSelection(DateTime appointmentDate, string appointmentTime, string slotIds, string redirectUrl)
        {
            _logger.LogInformation($"TreatmentId in session: {HttpContext.Session.GetInt32("SelectedTreatmentId")}");
            _logger.LogInformation($"DoctorId in session: {HttpContext.Session.GetInt32("SelectedDoctorId")}");
            
            if (appointmentDate < DateTime.UtcNow.Date)
            {
                _logger.LogWarning($"Attempted to select past date: {appointmentDate}");
                return RedirectToAction("SelectDateTime");
            }

                        if (appointmentDate.Kind != DateTimeKind.Utc)
            {
                appointmentDate = DateTime.SpecifyKind(appointmentDate, DateTimeKind.Utc);
            }

                        HttpContext.Session.SetString("AppointmentDate", appointmentDate.ToString("yyyy-MM-dd"));
            HttpContext.Session.SetString("AppointmentTime", appointmentTime);
            
                        if (!string.IsNullOrEmpty(slotIds))
            {
                HttpContext.Session.SetString("SlotIds", slotIds);
                _logger.LogInformation($"Stored slot IDs: {slotIds}");
            }
            
                        _logger.LogInformation($"Stored date: {appointmentDate.ToString("yyyy-MM-dd")}, time: {appointmentTime}");
            
                        return RedirectToAction("Confirm");
        }
        
                public async Task<IActionResult> Confirm()
        {
                        _logger.LogInformation("Starting Confirm method");
            
                        var treatmentId = HttpContext.Session.GetInt32("SelectedTreatmentId");
            var doctorId = HttpContext.Session.GetInt32("SelectedDoctorId");
            var appointmentDate = HttpContext.Session.GetString("AppointmentDate");
            var appointmentTime = HttpContext.Session.GetString("AppointmentTime");
            
            _logger.LogInformation($"Session values - TreatmentId: {treatmentId}, DoctorId: {doctorId}, Date: {appointmentDate}, Time: {appointmentTime}");
            
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
            
            var bookingModel = new AppointmentBookingViewModel();
            
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
            
            if (DateTime.TryParse(appointmentDate, out DateTime parsedDate))
            {
                bookingModel.AppointmentDate = parsedDate;
                ViewData["AppointmentDate"] = parsedDate.ToString("dddd, MMMM d, yyyy");
            }
            
            bookingModel.AppointmentTime = appointmentTime;
            
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
            _logger.LogInformation($"SubmitBooking started with TreatmentId={model.TreatmentId}, DoctorId={model.DoctorId}");
            _logger.LogInformation($"AppointmentDate={model.AppointmentDate}, AppointmentTime={model.AppointmentTime}");
            
            ModelState.Clear(); 
            
            if (model.TreatmentId <= 0)
            {
                ModelState.AddModelError("TreatmentId", "Treatment is required");
            }
            
            if (model.DoctorId <= 0)
            {
                ModelState.AddModelError("DoctorId", "Doctor is required");
            }
            
            if (model.AppointmentDate == default)
            {
                ModelState.AddModelError("AppointmentDate", "Appointment date is required");
            }
            
            if (string.IsNullOrEmpty(model.AppointmentTime))
            {
                ModelState.AddModelError("AppointmentTime", "Appointment time is required");
            }
            
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogError($"Validation error for {state.Key}: {error.ErrorMessage}");
                    }
                }
                
                await RepopulateViewModelData(model);
                
                return View("Confirm", model);
            }
            
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }
                
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
                if (patient == null)
                {
                    ModelState.AddModelError("", "Patient record not found");
                    return View("Confirm", model);
                }
                
                var treatment = await _context.TreatmentTypes.FindAsync(model.TreatmentId);
                if (treatment == null)
                {
                    ModelState.AddModelError("", "Selected treatment not found");
                    return View("Confirm", model);
                }
                
                decimal depositAmount = treatment.Price * 0.3m;
                
                var appointment = new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = model.DoctorId,
                    TreatmentTypeId = model.TreatmentId,
                    AppointmentDate = DateTime.SpecifyKind(model.AppointmentDate, DateTimeKind.Utc),
                    AppointmentTime = TimeSpan.Parse(model.AppointmentTime),
                    Duration = treatment.Duration,
                    Notes = model.Notes ?? "",
                    Status = "Pending-Payment", 
                    CreatedAt = DateTime.UtcNow,
                    PaymentStatus = PaymentStatus.Pending,
                    DepositAmount = depositAmount,
                    TotalAmount = treatment.Price
                };
                
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                
                string successUrl = BuildActionUrl(
                    "BookingSuccess",
                    "Appointments",
                    new { area = "Patient", id = appointment.Id }
                );
                
                string cancelUrl = BuildActionUrl(
                    "Details",
                    "Appointments",
                    new { area = "Patient", id = appointment.Id }
                );
                
                string failureUrl = BuildActionUrl(
                    "BookingFailure",
                    "Appointments",
                    new { area = "Patient", id = appointment.Id }
                );
                
                var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                    appointment.Id, 
                    depositAmount, 
                    successUrl, 
                    cancelUrl,
                    failureUrl
                );
                
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
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
                if (patient == null)
                {
                    return RedirectToAction("Index");
                }

                HttpContext.Session.Clear();


                _logger.LogInformation("Booking process cancelled and session state cleared");

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelBooking method");
                return RedirectToAction("Index");
            }
        }

        private async Task RepopulateViewModelData(AppointmentBookingViewModel model)
        {
            try
            {
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
                
                var user = await _userManager.GetUserAsync(User);
                if (user == null || appointment.Patient.UserID != user.Id)
                {
                    _logger.LogWarning($"Unauthorized access to appointment {id}");
                    return RedirectToAction("Index");
                }
                
                ViewData["AppointmentId"] = appointment.Id;
                ViewData["AppointmentDate"] = appointment.AppointmentDate.ToString("dddd, MMMM d, yyyy");
                
                bool isPM = appointment.AppointmentTime.Hours >= 12;
                int hour12 = appointment.AppointmentTime.Hours % 12;
                if (hour12 == 0) hour12 = 12;
                string formattedTime = $"{hour12}:{appointment.AppointmentTime.Minutes:D2} {(isPM ? "PM" : "AM")}";
                ViewData["AppointmentTime"] = formattedTime;
                
                ViewData["DoctorName"] = $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}";
                ViewData["TreatmentName"] = appointment.TreatmentType.Name;
                ViewData["TreatmentCost"] = $"RM {appointment.TreatmentType.Price:0.00}";
                ViewData["DepositAmount"] = $"RM {(appointment.TreatmentType.Price * 0.3m):0.00}";
                
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
                
                var user = await _userManager.GetUserAsync(User);
                if (user == null || appointment.Patient.UserID != user.Id)
                {
                    _logger.LogWarning($"Unauthorized access to appointment {id}");
                    return RedirectToAction("Index");
                }
                
                ViewData["AppointmentId"] = appointment.Id;
                ViewData["AppointmentDate"] = appointment.AppointmentDate.ToString("dddd, MMMM d, yyyy");
                
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
            
            var user = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
            
            if (patient == null || appointment.PatientId != patient.Id)
            {
                return Forbid();
            }
            
            var today = DateTime.UtcNow.Date;
            var canCancel = appointment.Status != "Cancelled" && appointment.Status != "Completed" && 
                        (appointment.AppointmentDate > today || 
                        (appointment.AppointmentDate == today && appointment.AppointmentTime > DateTime.UtcNow.TimeOfDay));
                        
            if (!canCancel)
            {
                TempData["ErrorMessage"] = "This appointment cannot be cancelled.";
                return RedirectToAction(nameof(Index));
            }
            
            var previousStatus = appointment.Status;
            
            appointment.Status = "Cancelled";
            appointment.UpdatedAt = DateTime.UtcNow;
            
            var associatedTimeSlots = await _context.TimeSlots
                .Where(ts => ts.AppointmentId == appointment.Id)
                .ToListAsync();
            
            foreach (var timeSlot in associatedTimeSlots)
            {
                timeSlot.IsBooked = false;
                timeSlot.AppointmentId = null;
            }
            
            await _context.SaveChangesAsync();
            
            string formattedDate = appointment.AppointmentDate.ToString("MMMM d, yyyy");
            
            bool isPM = appointment.AppointmentTime.Hours >= 12;
            int hour12 = appointment.AppointmentTime.Hours % 12;
            if (hour12 == 0) hour12 = 12;
            string formattedTime = $"{hour12}:{appointment.AppointmentTime.Minutes:D2} {(isPM ? "PM" : "AM")}";
            
            string refundMessage = "";

            if (appointment.PaymentStatus == PaymentStatus.Pending)
            {
                refundMessage = "This appointment was in pending status and has been cancelled.";
            }
            else
            {
                bool isEligibleForRefund = false;
                bool refundProcessed = false;
                decimal refundAmount = 0;

                try 
                {
                    DateTime appointmentDateTime = appointment.AppointmentDate.Date + appointment.AppointmentTime;
                    isEligibleForRefund = (appointmentDateTime - DateTime.UtcNow).TotalHours > 24;
                    
                    if (isEligibleForRefund)
                    {
                        var depositPayment = appointment.Payments
                            .FirstOrDefault(p => p.PaymentType == PaymentType.Deposit && p.Status == "succeeded");
                        
                        if (depositPayment != null)
                        {
                            refundAmount = depositPayment.Amount;
                            
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
            
            if (previousStatus == "Scheduled" || previousStatus == "Confirmed")
            {
                try
                {
                    var preferences = await _context.UserNotificationPreferences
                        .FirstOrDefaultAsync(p => p.UserId == user.Id);
                        
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
                    
                    if (preferences.EmailAppointmentChanges)
                    {
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
                        
                        await _emailService.SendAppointmentCancellationEmailAsync(
                            user.Email,
                            $"{patient.FirstName} {patient.LastName}",
                            appointmentDetails);
                        
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
                    _logger.LogError(ex, "Error handling appointment cancellation notification");
                }
            }
            
            TempData["SuccessMessage"] = "Your appointment has been cancelled.";
            return RedirectToAction(nameof(Index));
        }

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
        
        public async Task<IActionResult> Details(int id)
        {
            var today = DateTime.UtcNow.Date;
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.TreatmentType)
                .Include(a => a.Payments) 
                .FirstOrDefaultAsync(a => a.Id == id);
                    
            if (appointment == null)
            {
                return NotFound();
            }
            
            var user = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
            
            if (patient == null || appointment.PatientId != patient.Id)
            {
                return Forbid();
            }
            
            DateTime? depositPaymentDate = null;
            DateTime? fullPaymentDate = null;
            decimal amountPaid = 0;
            
            if (appointment.Payments != null && appointment.Payments.Any())
            {
                foreach (var payment in appointment.Payments.Where(p => p.Status == "succeeded"))
                {
                    amountPaid += payment.Amount;
                    
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
            
            decimal remainingAmount = appointment.TotalAmount - amountPaid;
            
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
            var slots = await _context.TimeSlots
                .Where(ts => slotIds.Contains(ts.Id))
                .ToListAsync();
            
            foreach (var slot in slots)
            {
                slot.IsBooked = true;
                slot.AppointmentId = appointmentId;
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task<List<int>> FindConsecutiveSlotIdsAsync(int doctorId, DateTime appointmentDate, TimeSpan appointmentTime, int durationMinutes)
        {
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
            
            int requiredSlots = (int)Math.Ceiling(durationMinutes / 60.0);
            
            var slotIds = new List<int> { startSlot.Id };
            
            if (requiredSlots > 1)
            {
                var nextStartTime = startSlot.EndTime;
                
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
                
                var preferences = await _context.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);
                    
                if (preferences == null)
                {
                    preferences = new UserNotificationPreferences
                    {
                        UserId = user.Id,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.UserNotificationPreferences.Add(preferences);
                }
                
                preferences.EmailAppointmentReminders = model.EmailAppointmentReminders;
                preferences.EmailNewAppointments = model.EmailNewAppointments;
                preferences.EmailAppointmentChanges = model.EmailAppointmentChanges;
                preferences.EmailPromotions = model.EmailPromotions;
                preferences.LastUpdated = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Notification preferences updated successfully.";
                
                return RedirectToAction("NotificationSettings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification preferences");
                TempData["ErrorMessage"] = "Failed to update notification preferences.";
                return RedirectToAction("NotificationSettings");
            }
        }

        public async Task<IActionResult> NotificationSettings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            var preferences = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
                
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
        var appointment = await _context.Appointments
            .Include(a => a.TimeSlots)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (appointment == null)
        {
            return Json(new { isAvailable = false });
        }

        var slotIds = appointment.TimeSlots.Select(ts => ts.Id).ToList();
        
        var availableSlots = await _context.TimeSlots
            .Where(ts => slotIds.Contains(ts.Id))
            .ToListAsync();
        
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
            var appointment = await _context.Appointments
                .Include(a => a.TreatmentType)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
            
            if (patient == null || appointment.PatientId != patient.Id)
            {
                return Forbid();
            }

            decimal depositAmount = appointment.TreatmentType.Price * 0.3m;
            
            string successUrl = BuildActionUrl(
                "BookingSuccess",
                "Appointments",
                new { area = "Patient", id = appointment.Id }
            );
            
            string cancelUrl = BuildActionUrl(
                "Details",
                "Appointments",
                new { area = "Patient", id = appointment.Id }
            );
            
            string failureUrl = BuildActionUrl(
                "BookingFailure",
                "Appointments",
                new { area = "Patient", id = appointment.Id }
            );
            
            _logger.LogInformation($"Payment URLs - Success: {successUrl}, Cancel: {cancelUrl}, Failure: {failureUrl}");
            
            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                appointment.Id, 
                depositAmount, 
                successUrl, 
                cancelUrl,
                failureUrl
            );
            _logger.LogInformation($"Stripe checkout URL: {checkoutUrl}");
            
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
            var appointment = await _context.Appointments
                .Include(a => a.TreatmentType)
                .Include(a => a.Payments)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
            
            if (patient == null || appointment.PatientId != patient.Id)
            {
                return Forbid();
            }

            decimal amountPaid = appointment.Payments
                .Where(p => p.Status == "succeeded")
                .Sum(p => p.Amount);
            
            decimal remainingAmount = appointment.TotalAmount - amountPaid;
            
            if (remainingAmount <= 0)
            {
                TempData["InfoMessage"] = "This appointment has already been fully paid.";
                return RedirectToAction("Details", new { id = appointment.Id });
            }

            string successUrl = BuildActionUrl(
                "PaymentSuccess",
                "Appointments",
                new { area = "Patient", id = appointment.Id, type = "remaining" }
            );
            
            string cancelUrl = BuildActionUrl(
                "Details",
                "Appointments",
                new { area = "Patient", id = appointment.Id }
            );
            
            string failureUrl = BuildActionUrl(
                "BookingFailure",
                "Appointments",
                new { area = "Patient", id = appointment.Id, error = "Your remaining payment could not be processed." }
            );
            
            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                appointment.Id, 
                remainingAmount, 
                successUrl, 
                cancelUrl,
                failureUrl,
                PaymentType.FullPayment
            );
            
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

            if (type == "remaining")
            {
                appointment.Status = "Completed"; 
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
    [HttpGet]
    public async Task<IActionResult> GetTreatmentReport(int id)
    {
        try
        {
            _logger.LogInformation($"GetTreatmentReport called for appointment ID: {id}");
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Unauthorized access: User not authenticated");
                return Unauthorized();
            }

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
            if (patient == null)
            {
                _logger.LogWarning($"Unauthorized access: Patient record not found for user {user.Id}");
                return Unauthorized();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == patient.Id);
                
            if (appointment == null)
            {
                _logger.LogWarning($"Patient attempted to access appointment {id} that doesn't exist or doesn't belong to them");
                return NotFound();
            }

            if (appointment.Status != "Completed")
            {
                _logger.LogWarning($"Patient attempted to access report for non-completed appointment {id}");
                return BadRequest(new { message = "Reports are only available for completed appointments" });
            }

            var treatmentReport = await _context.TreatmentReports
                .Include(tr => tr.Doctor)
                .FirstOrDefaultAsync(tr => tr.AppointmentId == id);

            if (treatmentReport == null)
            {
                _logger.LogInformation($"No treatment report found for appointment {id}");
                return Json(new { exists = false });
            }

            if (string.IsNullOrWhiteSpace(treatmentReport.Notes))
            {
                _logger.LogInformation($"Treatment report found for appointment {id}, but notes are empty");
                return Json(new { 
                    id = treatmentReport.Id,
                    appointmentId = treatmentReport.AppointmentId,
                    exists = true,
                    hasNotes = false
                });
            }

            _logger.LogInformation($"Returning treatment report with notes for appointment {id}");
            return Json(new {
                id = treatmentReport.Id,
                appointmentId = treatmentReport.AppointmentId,
                treatmentDate = treatmentReport.TreatmentDate,
                notes = treatmentReport.Notes,
                doctorName = $"Dr. {treatmentReport.Doctor.FirstName} {treatmentReport.Doctor.LastName}",
                createdAt = treatmentReport.CreatedAt,
                exists = true,
                hasNotes = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving treatment report for appointment {id}");
            return StatusCode(500, new { message = "An error occurred while retrieving the treatment report" });
        }
    }

    private string BuildActionUrl(string action, string controller, object routeValues)
    {
        var relativeUrl = Url.Action(action, controller, routeValues);
        
        _logger.LogInformation($"Generated relative URL: {relativeUrl}");
        
        if (relativeUrl != null && relativeUrl.StartsWith("http"))
        {
            _logger.LogWarning($"URL.Action returned an absolute URL: {relativeUrl}");
            return relativeUrl;
        }
        
        var baseUrl = Environment.GetEnvironmentVariable("APPLICATION_URL") ?? "http://localhost:5001";
        baseUrl = baseUrl.TrimEnd('/');
        
        if (relativeUrl != null && !relativeUrl.StartsWith("/"))
        {
            relativeUrl = "/" + relativeUrl;
        }
        
        var fullUrl = baseUrl + relativeUrl;
        
        _logger.LogInformation($"Final URL constructed: {fullUrl}");
        
        return fullUrl;
    }
    }
}