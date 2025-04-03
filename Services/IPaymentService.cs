using System.Threading.Tasks;
using DentalManagement.Models;

namespace DentalManagement.Services
{
    public interface IPaymentService
    {
        // Create a checkout session with default payment type (Deposit)
        Task<string> CreateCheckoutSessionAsync(int appointmentId, decimal amount, string successUrl, string cancelUrl);
        
        // Create a checkout session with specified payment type
        Task<string> CreateCheckoutSessionAsync(int appointmentId, decimal amount, string successUrl, string cancelUrl, PaymentType paymentType);
        
        // Verify a payment by its intent ID
        Task<bool> VerifyPaymentAsync(string paymentIntentId);
        
        // Process a refund for an appointment
        Task<bool> ProcessRefundAsync(int appointmentId);
        
        // Record a payment in the database
        Task<Payment> RecordPaymentAsync(int appointmentId, string paymentIntentId, decimal amount, 
                                         PaymentType paymentType, string status = "pending", string checkoutSessionId = null);
        
        // Record a refund in the database
        Task<Payment> RecordRefundAsync(int appointmentId, string refundId, decimal amount, string status = "pending");
        
        // Update payment status in the database
        Task UpdatePaymentStatusAsync(string paymentIntentId, string status, string errorMessage = null);
        
        // Update appointment payment status
        Task UpdateAppointmentPaymentStatusAsync(int appointmentId, PaymentStatus status);
    }
}