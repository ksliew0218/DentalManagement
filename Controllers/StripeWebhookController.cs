using System;
using System.IO;
using System.Threading.Tasks;
using DentalManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using DentalManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using DentalManagement.Areas.Patient.Models;

namespace DentalManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<StripeWebhookController> _logger;
        private readonly StripeSettings _stripeSettings;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<User> _userManager;
        
        public StripeWebhookController(
            IPaymentService paymentService,
            ILogger<StripeWebhookController> logger,
            IOptions<StripeSettings> stripeSettings,
            ApplicationDbContext context,
            IEmailService emailService,
            UserManager<User> userManager)
        {
            _paymentService = paymentService;
            _logger = logger;
            _stripeSettings = stripeSettings.Value;
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
        }
        
        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _stripeSettings.WebhookSecret
                );
                
                // Handle the event based on type
                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                        await HandleCheckoutSessionCompleted(session);
                        break;
                        
                    case "payment_intent.succeeded":
                        var intent = stripeEvent.Data.Object as PaymentIntent;
                        await HandlePaymentIntentSucceeded(intent);
                        break;
                        
                    case "payment_intent.payment_failed":
                        var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                        await HandlePaymentIntentFailed(failedIntent);
                        break;
                        
                    case "charge.refunded":
                        var charge = stripeEvent.Data.Object as Charge;
                        await HandleChargeRefunded(charge);
                        break;
                        
                    default:
                        _logger.LogInformation($"Unhandled event type: {stripeEvent.Type}");
                        break;
                }
                
                return Ok();
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Error processing Stripe webhook");
                return BadRequest();
            }
        }
        
        private async Task HandleCheckoutSessionCompleted(Stripe.Checkout.Session session)
        {
            _logger.LogInformation($"Checkout session {session.Id} completed");
            
            // Get the appointment ID from the session metadata
            if (session.Metadata.TryGetValue("AppointmentId", out string appointmentIdStr) && 
                int.TryParse(appointmentIdStr, out int appointmentId))
            {
                try 
                {
                    // Find the appointment with all related entities
                    var appointment = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.TreatmentType)
                        .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                        .FirstOrDefaultAsync(a => a.Id == appointmentId);
                    
                    if (appointment != null)
                    {
                        // Explicitly update appointment status
                        appointment.Status = "Confirmed";
                        appointment.PaymentStatus = PaymentStatus.PartiallyPaid;
                        appointment.UpdatedAt = DateTime.UtcNow;
                        
                        // Record the payment
                        var payment = await _paymentService.RecordPaymentAsync(
                            appointmentId, 
                            session.PaymentIntentId, 
                            decimal.Parse(session.AmountTotal.ToString()) / 100m, 
                            PaymentType.Deposit,
                            "succeeded",
                            session.Id
                        );
                        
                        // Book time slots
                        var slotIds = await FindConsecutiveSlotIdsAsync(
                            appointment.DoctorId,
                            appointment.AppointmentDate,
                            appointment.AppointmentTime,
                            appointment.Duration
                        );
                        
                        await BookTimeSlotsAsync(appointmentId, slotIds);
                        
                        // Save changes to the database
                        await _context.SaveChangesAsync();
                        
                        // Create and send notifications
                        await CreateAppointmentNotification(appointment);
                        
                        _logger.LogInformation($"Successfully processed checkout for appointment {appointmentId}");
                    }
                    else
                    {
                        _logger.LogWarning($"No appointment found with ID {appointmentId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing completed checkout for appointment {appointmentId}");
                }
            }
            else
            {
                _logger.LogWarning("No appointment ID found in Stripe checkout session metadata");
            }
        }
        
        private async Task CreateAppointmentNotification(Appointment appointment)
        {
            try 
            {
                // Ensure we have a user
                var user = appointment.Patient.User ?? 
                    await _userManager.FindByIdAsync(appointment.Patient.UserID);
                
                var treatment = appointment.TreatmentType;
                var doctor = appointment.Doctor;

                // Format appointment date and time
                string formattedDate = appointment.AppointmentDate.ToString("MMMM d, yyyy");
                bool isPM = appointment.AppointmentTime.Hours >= 12;
                int hour12 = appointment.AppointmentTime.Hours % 12;
                if (hour12 == 0) hour12 = 12;
                string formattedTime = $"{hour12}:{appointment.AppointmentTime.Minutes:D2} {(isPM ? "PM" : "AM")}";

                // Create in-app notification
                var notification = new UserNotification
                {
                    UserId = user.Id,
                    NotificationType = "Appointment_Confirmed",
                    Title = "Appointment Confirmed",
                    Message = $"Your {treatment.Name} appointment with Dr. {doctor.FirstName} {doctor.LastName} has been confirmed for {formattedDate} at {formattedTime}.",
                    RelatedEntityId = appointment.Id,
                    ActionController = "Appointments",
                    ActionName = "Details",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserNotifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send email if preferences allow
                var preferences = await _context.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);

                if (preferences?.EmailNewAppointments == true)
                {
                    var appointmentDetails = new AppointmentDetailViewModel
                    {
                        Id = appointment.Id,
                        TreatmentName = treatment.Name,
                        DoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}",
                        DoctorSpecialization = doctor.Specialty,
                        AppointmentDate = appointment.AppointmentDate,
                        AppointmentTime = appointment.AppointmentTime,
                        Status = appointment.Status,
                        CreatedOn = appointment.CreatedAt,
                        TreatmentCost = treatment.Price,
                        TreatmentDuration = treatment.Duration
                    };

                    await _emailService.SendAppointmentConfirmationEmailAsync(
                        user.Email, 
                        $"{appointment.Patient.FirstName} {appointment.Patient.LastName}", 
                        appointmentDetails
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating appointment notification");
            }
        }
        
        private async Task<List<int>> FindConsecutiveSlotIdsAsync(int doctorId, DateTime appointmentDate, TimeSpan appointmentTime, int durationMinutes)
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

        private async Task BookTimeSlotsAsync(int appointmentId, List<int> slotIds)
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
        
        private async Task HandlePaymentIntentSucceeded(PaymentIntent intent)
        {
            _logger.LogInformation($"Payment intent {intent.Id} succeeded");
            
            await _paymentService.UpdatePaymentStatusAsync(intent.Id, "succeeded");
        }
        
        private async Task HandlePaymentIntentFailed(PaymentIntent intent)
        {
            _logger.LogInformation($"Payment intent {intent.Id} failed");
            
            string errorMessage = intent.LastPaymentError?.Message;
            await _paymentService.UpdatePaymentStatusAsync(intent.Id, "failed", errorMessage);
        }
        
        private async Task HandleChargeRefunded(Charge charge)
        {
            _logger.LogInformation($"Charge {charge.Id} refunded");
            
            // Get the payment intent ID
            var paymentIntentId = charge.PaymentIntentId;
            
            if (!string.IsNullOrEmpty(paymentIntentId))
            {
                // Get the payment with this payment intent
                var payment = await _context.Payments
                    .Include(p => p.Appointment)
                    .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId);
                
                if (payment != null)
                {
                    // Update the appointment payment status
                    await _paymentService.UpdateAppointmentPaymentStatusAsync(
                        payment.AppointmentId, 
                        PaymentStatus.Refunded
                    );
                }
            }
        }
    }
}