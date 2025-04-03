using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
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
                    // Check if we already have a payment record for this session
                    var existingPayment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.CheckoutSessionId == session.Id);
                    
                    if (existingPayment != null)
                    {
                        // Update the existing payment record instead of creating a new one
                        existingPayment.PaymentIntentId = session.PaymentIntentId;
                        existingPayment.Status = "succeeded";
                        existingPayment.UpdatedAt = DateTime.UtcNow;
                        
                        // Get receipt URL from the charges
                        var chargeService = new ChargeService();
                        var charges = await chargeService.ListAsync(new ChargeListOptions 
                        { 
                            PaymentIntent = session.PaymentIntentId 
                        });
                        
                        if (charges?.Data?.Count > 0)
                        {
                            existingPayment.ReceiptUrl = charges.Data[0].ReceiptUrl;
                        }
                        
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // Extract payment type from metadata
                        PaymentType paymentType = PaymentType.Deposit; // Default
                        if (session.Metadata.TryGetValue("PaymentType", out string paymentTypeStr))
                        {
                            if (Enum.TryParse(paymentTypeStr, out PaymentType parsedType))
                            {
                                paymentType = parsedType;
                            }
                        }
                        
                        // Record the payment
                        var payment = await _paymentService.RecordPaymentAsync(
                            appointmentId, 
                            session.PaymentIntentId, 
                            decimal.Parse(session.AmountTotal.ToString()) / 100m, 
                            paymentType,
                            "succeeded",
                            session.Id
                        );
                    }
                    
                    // Find the appointment with all related entities
                    var appointment = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.TreatmentType)
                        .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                        .FirstOrDefaultAsync(a => a.Id == appointmentId);
                    
                    if (appointment != null)
                    {
                        // Update appointment payment status based on the payment type
                        PaymentType paymentType = PaymentType.Deposit; // Default
                        if (session.Metadata.TryGetValue("PaymentType", out string paymentTypeStr))
                        {
                            if (Enum.TryParse(paymentTypeStr, out PaymentType parsedType))
                            {
                                paymentType = parsedType;
                            }
                        }
                        
                        appointment.PaymentStatus = (paymentType == PaymentType.FullPayment) 
                            ? PaymentStatus.Paid 
                            : PaymentStatus.PartiallyPaid;
                        
                        // For deposit payments, update status to Confirmed and book slots
                        if (paymentType == PaymentType.Deposit && appointment.Status != "Confirmed")
                        {
                            appointment.Status = "Confirmed";
                            
                            // Book time slots for appointments
                            var slotIds = await FindConsecutiveSlotIdsAsync(
                                appointment.DoctorId,
                                appointment.AppointmentDate,
                                appointment.AppointmentTime,
                                appointment.Duration
                            );
                            
                            await BookTimeSlotsAsync(appointmentId, slotIds);
                            
                            // Create and send notifications
                            await CreateAppointmentNotification(appointment);
                        }
                        
                        appointment.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
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
            
            try
            {
                // Get or determine the payment type from metadata
                PaymentType paymentType = PaymentType.Deposit; // Default
                if (intent.Metadata.TryGetValue("PaymentType", out string paymentTypeStr))
                {
                    if (Enum.TryParse(paymentTypeStr, out PaymentType parsedType))
                    {
                        paymentType = parsedType;
                    }
                }
                
                // Check if a payment record already exists
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentIntentId == intent.Id);
                
                if (existingPayment != null && string.IsNullOrEmpty(existingPayment.ReceiptUrl))
                {
                    // Update the existing payment with receipt URL
                    var chargeService = new ChargeService();
                    var charges = await chargeService.ListAsync(new ChargeListOptions 
                    { 
                        PaymentIntent = intent.Id 
                    });
                    
                    if (charges?.Data?.Count > 0)
                    {
                        existingPayment.ReceiptUrl = charges.Data[0].ReceiptUrl;
                        existingPayment.Status = "succeeded";
                        existingPayment.UpdatedAt = DateTime.UtcNow;
                        
                        await _context.SaveChangesAsync();
                        
                        // Update appointment payment status
                        await _paymentService.UpdateAppointmentPaymentStatusAsync(
                            existingPayment.AppointmentId, 
                            existingPayment.PaymentType == PaymentType.FullPayment ? 
                                PaymentStatus.Paid : PaymentStatus.PartiallyPaid
                        );
                        
                        // For deposit payments, also check if we need to update the appointment status
                        if (existingPayment.PaymentType == PaymentType.Deposit)
                        {
                            var appointment = await _context.Appointments
                                .FindAsync(existingPayment.AppointmentId);
                                
                            if (appointment != null && appointment.Status == "Scheduled")
                            {
                                appointment.Status = "Confirmed";
                                appointment.UpdatedAt = DateTime.UtcNow;
                                await _context.SaveChangesAsync();
                            }
                        }
                        
                        _logger.LogInformation($"Updated payment {existingPayment.Id} with receipt URL");
                        return;
                    }
                }
                
                // Look for a pending payment with matching checkout session
                if (intent.Metadata.TryGetValue("CheckoutSessionId", out string checkoutSessionId))
                {
                    var pendingPayment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.CheckoutSessionId == checkoutSessionId);
                    
                    if (pendingPayment != null)
                    {
                        // Update the pending payment
                        pendingPayment.PaymentIntentId = intent.Id;
                        pendingPayment.Status = "succeeded";
                        pendingPayment.UpdatedAt = DateTime.UtcNow;
                        
                        // Get receipt URL
                        var chargeService = new ChargeService();
                        var charges = await chargeService.ListAsync(new ChargeListOptions 
                        { 
                            PaymentIntent = intent.Id 
                        });
                        
                        if (charges?.Data?.Count > 0)
                        {
                            pendingPayment.ReceiptUrl = charges.Data[0].ReceiptUrl;
                        }
                        
                        await _context.SaveChangesAsync();
                        
                        // Update appointment payment status
                        await _paymentService.UpdateAppointmentPaymentStatusAsync(
                            pendingPayment.AppointmentId, 
                            pendingPayment.PaymentType == PaymentType.FullPayment ? 
                                PaymentStatus.Paid : PaymentStatus.PartiallyPaid
                        );
                        
                        _logger.LogInformation($"Updated pending payment with checkout session {checkoutSessionId}");
                        return;
                    }
                }
                
                // If we couldn't find an existing payment record, use the generic method
                await _paymentService.UpdatePaymentStatusAsync(intent.Id, "succeeded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling payment intent success for {intent.Id}");
                // Fall back to the generic method
                await _paymentService.UpdatePaymentStatusAsync(intent.Id, "succeeded");
            }
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