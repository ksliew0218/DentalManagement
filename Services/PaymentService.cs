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

            _logger.LogInformation("Stripe payment service initialized");
            _logger.LogInformation($"Using currency: {_stripeSettings.Currency ?? "myr"}");
            
            _logger.LogInformation($"Secret key exists: {!string.IsNullOrEmpty(_stripeSettings.SecretKey)}");
            _logger.LogInformation($"Publishable key exists: {!string.IsNullOrEmpty(_stripeSettings.PublishableKey)}");
            _logger.LogInformation($"Webhook secret exists: {!string.IsNullOrEmpty(_stripeSettings.WebhookSecret)}");
        }
        
        public async Task<string> CreateCheckoutSessionAsync(int appointmentId, decimal amount, string successUrl, string cancelUrl)
        {
            return await CreateCheckoutSessionAsync(appointmentId, amount, successUrl, cancelUrl, null, PaymentType.Deposit);
        }
        
        public async Task<string> CreateCheckoutSessionAsync(int appointmentId, decimal amount, string successUrl, string cancelUrl, PaymentType paymentType)
        {
            return await CreateCheckoutSessionAsync(appointmentId, amount, successUrl, cancelUrl, null, paymentType);
        }
        
        public async Task<string> CreateCheckoutSessionAsync(int appointmentId, decimal amount, string successUrl, string cancelUrl, string failureUrl)
        {
            return await CreateCheckoutSessionAsync(appointmentId, amount, successUrl, cancelUrl, failureUrl, PaymentType.Deposit);
        }
        
        public async Task<string> CreateCheckoutSessionAsync(int appointmentId, decimal amount, string successUrl, string cancelUrl, string failureUrl, PaymentType paymentType)
        {
            try
            {
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
                
                var amountInCents = (long)(amount * 100);
                
                string paymentTitle, paymentDescription;
                
                if (paymentType == PaymentType.Deposit)
                {
                    paymentTitle = $"30% Deposit for {appointment.TreatmentType.Name}";
                    paymentDescription = $"Appointment on {appointment.AppointmentDate.ToString("MMMM d, yyyy")} at {FormatTime(appointment.AppointmentTime)}";
                }
                else if (paymentType == PaymentType.FullPayment)
                {
                    paymentTitle = $"Remaining Balance for {appointment.TreatmentType.Name}";
                    paymentDescription = $"Appointment on {appointment.AppointmentDate.ToString("MMMM d, yyyy")} at {FormatTime(appointment.AppointmentTime)}";
                }
                else
                {
                    paymentTitle = $"Payment for {appointment.TreatmentType.Name}";
                    paymentDescription = $"Appointment on {appointment.AppointmentDate.ToString("MMMM d, yyyy")} at {FormatTime(appointment.AppointmentTime)}";
                }
                
                var lineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = amountInCents,
                            Currency = _stripeSettings.Currency ?? "myr", 
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = paymentTitle,
                                Description = paymentDescription
                            }
                        },
                        Quantity = 1
                    }
                };
                
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    CustomerEmail = appointment.Patient.User?.Email,
                    PaymentIntentData = new SessionPaymentIntentDataOptions
                    {
                        Metadata = new Dictionary<string, string>
                        {
                            { "AppointmentId", appointmentId.ToString() },
                            { "PaymentType", paymentType.ToString() },
                            { "IsRemainingPayment", (paymentType == PaymentType.FullPayment).ToString() }
                        }
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "AppointmentId", appointmentId.ToString() },
                        { "PatientId", appointment.PatientId.ToString() },
                        { "DoctorName", $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}" },
                        { "TreatmentType", appointment.TreatmentType.Name },
                        { "AppointmentDate", appointment.AppointmentDate.ToString("yyyy-MM-dd") },
                        { "AppointmentTime", appointment.AppointmentTime.ToString() },
                        { "PaymentType", paymentType.ToString() }
                    }
                };
                
                if (!string.IsNullOrEmpty(failureUrl))
                {
                    options.Metadata.Add("FailureUrl", failureUrl);
                    options.PaymentIntentData.Metadata.Add("FailureUrl", failureUrl);
                }
                
                var service = new SessionService();
                var session = await service.CreateAsync(options);
                
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.CheckoutSessionId == session.Id);
                
                if (existingPayment != null)
                {
                    _logger.LogWarning($"Payment with checkout session ID {session.Id} already exists");
                    return session.Url;
                }
                
                await RecordPaymentAsync(
                    appointmentId, 
                    null, 
                    amount,
                    paymentType,
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
                
                var refundAmount = payment.Amount;
                
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = payment.PaymentIntentId,
                    Amount = (long)(refundAmount * 100), 
                    Metadata = new Dictionary<string, string>
                    {
                        { "AppointmentId", appointmentId.ToString() },
                        { "Reason", "Appointment Cancellation" }
                    }
                };
                
                var refundService = new RefundService();
                var refund = await refundService.CreateAsync(refundOptions);
                
                await RecordRefundAsync(
                    appointmentId,
                    refund.Id,
                    refundAmount,
                    refund.Status
                );
                
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
                if (!string.IsNullOrEmpty(checkoutSessionId))
                {
                    var existingPayment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.CheckoutSessionId == checkoutSessionId);
                    
                    if (existingPayment != null)
                    {
                        _logger.LogInformation($"Updating existing payment for checkout session {checkoutSessionId}");
                        
                        existingPayment.PaymentIntentId = paymentIntentId ?? existingPayment.PaymentIntentId;
                        existingPayment.Amount = amount;
                        existingPayment.Status = status;
                        existingPayment.UpdatedAt = DateTime.UtcNow;
                        
                        await _context.SaveChangesAsync();
                        
                        if (status == "succeeded")
                        {
                            await UpdateAppointmentPaymentStatusAsync(
                                appointmentId, 
                                paymentType == PaymentType.FullPayment ? 
                                    PaymentStatus.Paid : PaymentStatus.PartiallyPaid
                            );
                        }
                        
                        return existingPayment;
                    }
                }
                
                if (!string.IsNullOrEmpty(paymentIntentId))
                {
                    var existingPayment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId);
                    
                    if (existingPayment != null)
                    {
                        _logger.LogInformation($"Updating existing payment for payment intent {paymentIntentId}");
                        
                        existingPayment.CheckoutSessionId = checkoutSessionId ?? existingPayment.CheckoutSessionId;
                        existingPayment.Amount = amount;
                        existingPayment.Status = status;
                        existingPayment.UpdatedAt = DateTime.UtcNow;
                        
                        await _context.SaveChangesAsync();
                        
                        if (status == "succeeded")
                        {
                            await UpdateAppointmentPaymentStatusAsync(
                                appointmentId, 
                                paymentType == PaymentType.FullPayment ? 
                                    PaymentStatus.Paid : PaymentStatus.PartiallyPaid
                            );
                        }
                        
                        return existingPayment;
                    }
                }
                
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
                    var paymentIntentService = new PaymentIntentService();
                    var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);
                    
                    if (paymentIntent.Metadata.TryGetValue("CheckoutSessionId", out string checkoutSessionId))
                    {
                        payment = await _context.Payments
                            .FirstOrDefaultAsync(p => p.CheckoutSessionId == checkoutSessionId);
                    }
                    
                    if (payment == null && paymentIntent.Metadata.TryGetValue("AppointmentId", out string appointmentIdStr) &&
                        int.TryParse(appointmentIdStr, out int appointmentId))
                    {
                        PaymentType paymentType = PaymentType.Deposit;
                        if (paymentIntent.Metadata.TryGetValue("PaymentType", out string paymentTypeStr))
                        {
                            Enum.TryParse(paymentTypeStr, out paymentType);
                        }
                        
                        payment = await _context.Payments
                            .Where(p => p.AppointmentId == appointmentId && 
                                   p.PaymentType == paymentType && 
                                   p.Status == "pending")
                            .OrderByDescending(p => p.CreatedAt)
                            .FirstOrDefaultAsync();
                    }
                    
                    if (payment == null)
                    {
                        _logger.LogWarning($"Payment with intent ID {paymentIntentId} not found");
                        return;
                    }
                }
                payment.Status = status;
                payment.ErrorMessage = errorMessage;
                payment.UpdatedAt = DateTime.UtcNow;
                
                if (string.IsNullOrEmpty(payment.PaymentIntentId))
                {
                    payment.PaymentIntentId = paymentIntentId;
                }
                
                if (status == "succeeded" && string.IsNullOrEmpty(payment.ReceiptUrl))
                {
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
                
                if (status == PaymentStatus.PartiallyPaid && appointment.Status == "Scheduled")
                {
                    appointment.Status = "Confirmed";
                }
                
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