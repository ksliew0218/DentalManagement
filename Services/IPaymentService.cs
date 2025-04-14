using System.Threading.Tasks;
using DentalManagement.Models;

namespace DentalManagement.Services
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSessionAsync(int appointmentId, decimal amount, string successUrl, string cancelUrl);
        
        Task<string> CreateCheckoutSessionAsync(int appointmentId, decimal amount, string successUrl, string cancelUrl, PaymentType paymentType);
        
        Task<string> CreateCheckoutSessionAsync(int appointmentId, decimal amount, string successUrl, string cancelUrl, string failureUrl);
        
        Task<string> CreateCheckoutSessionAsync(int appointmentId, decimal amount, string successUrl, string cancelUrl, string failureUrl, PaymentType paymentType);
        
        Task<bool> VerifyPaymentAsync(string paymentIntentId);
        
        Task<bool> ProcessRefundAsync(int appointmentId);
        
        Task<Payment> RecordPaymentAsync(int appointmentId, string paymentIntentId, decimal amount, 
                                         PaymentType paymentType, string status = "pending", string checkoutSessionId = null);
        
        Task<Payment> RecordRefundAsync(int appointmentId, string refundId, decimal amount, string status = "pending");
        
        Task UpdatePaymentStatusAsync(string paymentIntentId, string status, string errorMessage = null);
        
        Task UpdateAppointmentPaymentStatusAsync(int appointmentId, PaymentStatus status);
    }
}