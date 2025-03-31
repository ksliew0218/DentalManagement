using System;
using System.Threading.Tasks;
using DentalManagement.Models;

namespace DentalManagement.Services
{
    public interface IPaymentService
    {
        // Create a checkout session for the deposit payment
        Task<string> CreateCheckoutSessionAsync(int appointmentId, decimal amount, string successUrl, string cancelUrl);
        
        // Verify a payment status from Stripe
        Task<bool> VerifyPaymentAsync(string paymentIntentId);
        
        // Process a refund for a cancelled appointment
        Task<bool> ProcessRefundAsync(int appointmentId);
        
        // Record a payment in our database
        Task<Payment> RecordPaymentAsync(int appointmentId, string paymentIntentId, decimal amount, PaymentType paymentType, string status = "pending", string checkoutSessionId = null);
        
        // Record a refund in our database
        Task<Payment> RecordRefundAsync(int appointmentId, string refundId, decimal amount, string status = "pending");
        
        // Update a payment status in our database
        Task UpdatePaymentStatusAsync(string paymentIntentId, string status, string errorMessage = null);
        
        // Update an appointment's payment status
        Task UpdateAppointmentPaymentStatusAsync(int appointmentId, PaymentStatus status);
    }
}