using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DentalManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace DentalManagement.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly StripeSettings _stripeSettings;
        
        public PaymentService(
            ApplicationDbContext context,
            ILogger<PaymentService> logger,
            IOptions<StripeSettings> stripeSettings)
        {
            _context = context;
            _logger = logger;
            _stripeSettings = stripeSettings.Value;

            // Add these logging statements to verify configuration
            _logger.LogInformation("Stripe payment service initialized");
            _logger.LogInformation($"Using currency: {_stripeSettings.Currency ?? "myr"}");
            
            // Don't log the full keys but confirm they exist
            _logger.LogInformation($"Secret key exists: {!string.IsNullOrEmpty(_stripeSettings.SecretKey)}");
            _logger.LogInformation($"Publishable key exists: {!string.IsNullOrEmpty(_stripeSettings.PublishableKey)}");
            _logger.LogInformation($"Webhook secret exists: {!string.IsNullOrEmpty(_stripeSettings.WebhookSecret)}");
        }
        
        public async Task<string> CreateCheckoutSessionAsync(int appointmentId, decimal amount, string successUrl, string cancelUrl)
        {
            try
            {
                // Get appointment details for the metadata
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Include(a => a.TreatmentType)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);
                
                if (appointment == null)
                {
                    _logger.LogError($"Appointment {appointmentId} not found when creating checkout session");
                    throw new ArgumentException($"Appointment with ID {appointmentId} not found.");
                }
                
                // Convert amount to cents/smallest currency unit for Stripe
                var amountInCents = (long)(amount * 100);
                
                // Create line items for checkout - Update description to clearly indicate it's a deposit
                var lineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = amountInCents,
                            Currency = _stripeSettings.Currency ?? "myr", // Use the configured currency
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"30% Deposit for {appointment.TreatmentType.Name}",
                                Description = $"Appointment on {appointment.AppointmentDate.ToString("MMMM d, yyyy")} at {FormatTime(appointment.AppointmentTime)}"
                            }
                        },
                        Quantity = 1
                    }
                };
                
                // Create checkout session options
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    CustomerEmail = appointment.Patient.User?.Email,
                    Metadata = new Dictionary<string, string>
                    {
                        { "AppointmentId", appointmentId.ToString() },
                        { "PatientId", appointment.PatientId.ToString() },
                        { "DoctorName", $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}" },
                        { "TreatmentType", appointment.TreatmentType.Name },
                        { "AppointmentDate", appointment.AppointmentDate.ToString("yyyy-MM-dd") },
                        { "AppointmentTime", appointment.AppointmentTime.ToString() }
                    }
                };
                
                // Create the checkout session
                var service = new SessionService();
                var session = await service.CreateAsync(options);
                
                // Record the initial pending payment in our database
                await RecordPaymentAsync(
                    appointmentId, 
                    null, // PaymentIntentId will be updated after successful payment
                    amount,
                    PaymentType.Deposit,
                    "pending",
                    session.Id
                );
                
                return session.Url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating checkout session for appointment {appointmentId}");
                throw;
            }
        }
        
        public async Task<bool> VerifyPaymentAsync(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);
                
                return paymentIntent.Status == "succeeded";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying payment for intent {paymentIntentId}");
                return false;
            }
        }
        
        public async Task<bool> ProcessRefundAsync(int appointmentId)
        {
            try
            {
                // Get the payment to refund
                var payment = await _context.Payments
                    .Where(p => p.AppointmentId == appointmentId && 
                           p.Status == "succeeded" && 
                           p.PaymentType == PaymentType.Deposit)
                    .FirstOrDefaultAsync();
                
                if (payment == null || string.IsNullOrEmpty(payment.PaymentIntentId))
                {
                    _logger.LogWarning($"No successful payment found for appointment {appointmentId} to refund");
                    return false;
                }
                
                // Calculate refund amount (for now, refund the full deposit)
                var refundAmount = payment.Amount;
                
                // Create the refund on Stripe
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = payment.PaymentIntentId,
                    Amount = (long)(refundAmount * 100), // Convert to cents
                    Metadata = new Dictionary<string, string>
                    {
                        { "AppointmentId", appointmentId.ToString() },
                        { "Reason", "Appointment Cancellation" }
                    }
                };
                
                var refundService = new RefundService();
                var refund = await refundService.CreateAsync(refundOptions);
                
                // Record the refund in our database
                await RecordRefundAsync(
                    appointmentId,
                    refund.Id,
                    refundAmount,
                    refund.Status
                );
                
                // Update appointment payment status
                if (refund.Status == "succeeded")
                {
                    await UpdateAppointmentPaymentStatusAsync(appointmentId, PaymentStatus.Refunded);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing refund for appointment {appointmentId}");
                return false;
            }
        }
        
        public async Task<Payment> RecordPaymentAsync(int appointmentId, string paymentIntentId, decimal amount, 
                                                     PaymentType paymentType, string status = "pending", string checkoutSessionId = null)
        {
            try
            {
                var payment = new Payment
                {
                    AppointmentId = appointmentId,
                    PaymentIntentId = paymentIntentId,
                    CheckoutSessionId = checkoutSessionId,
                    Amount = amount,
                    PaymentType = paymentType,
                    Status = status,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                
                // Update appointment payment status if successful
                if (status == "succeeded")
                {
                    await UpdateAppointmentPaymentStatusAsync(
                        appointmentId, 
                        paymentType == PaymentType.FullPayment ? 
                            PaymentStatus.Paid : PaymentStatus.PartiallyPaid
                    );
                }
                
                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error recording payment for appointment {appointmentId}");
                throw;
            }
        }
        
        public async Task<Payment> RecordRefundAsync(int appointmentId, string refundId, decimal amount, string status = "pending")
        {
            try
            {
                var payment = new Payment
                {
                    AppointmentId = appointmentId,
                    RefundId = refundId,
                    Amount = amount,
                    PaymentType = PaymentType.Refund,
                    Status = status,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                
                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error recording refund for appointment {appointmentId}");
                throw;
            }
        }
        
        public async Task UpdatePaymentStatusAsync(string paymentIntentId, string status, string errorMessage = null)
        {
            try
            {
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId);
                
                if (payment == null)
                {
                    _logger.LogWarning($"Payment with intent ID {paymentIntentId} not found");
                    return;
                }
                
                payment.Status = status;
                payment.ErrorMessage = errorMessage;
                payment.UpdatedAt = DateTime.UtcNow;
                
                // If it's a successful payment and we need to update the receipt URL
                if (status == "succeeded" && string.IsNullOrEmpty(payment.ReceiptUrl))
                {
                    // Instead of getting Charges from PaymentIntent, retrieve the charge directly
                    var chargeService = new ChargeService();
                    var charges = await chargeService.ListAsync(new ChargeListOptions 
                    { 
                        PaymentIntent = paymentIntentId 
                    });
                    
                    if (charges?.Data?.Count > 0)
                    {
                        payment.ReceiptUrl = charges.Data[0].ReceiptUrl;
                    }
                }
                
                await _context.SaveChangesAsync();
                
                // Update appointment payment status if successful
                if (status == "succeeded")
                {
                    var appointmentId = payment.AppointmentId;
                    await UpdateAppointmentPaymentStatusAsync(
                        appointmentId, 
                        payment.PaymentType == PaymentType.FullPayment ? 
                            PaymentStatus.Paid : PaymentStatus.PartiallyPaid
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating payment status for intent {paymentIntentId}");
            }
        }
        
        public async Task UpdateAppointmentPaymentStatusAsync(int appointmentId, PaymentStatus status)
        {
            try
            {
                var appointment = await _context.Appointments
                    .FindAsync(appointmentId);
                
                if (appointment == null)
                {
                    _logger.LogWarning($"Appointment {appointmentId} not found when updating payment status");
                    return;
                }
                
                appointment.PaymentStatus = status;
                appointment.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating appointment {appointmentId} payment status");
            }
        }
        
        private string FormatTime(TimeSpan time)
        {
            bool isPM = time.Hours >= 12;
            int hour12 = time.Hours % 12;
            if (hour12 == 0) hour12 = 12;
            return $"{hour12}:{time.Minutes:D2} {(isPM ? "PM" : "AM")}";
        }
    }
}