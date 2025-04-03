using System;
using System.Collections.Generic;
using DentalManagement.Models;

namespace DentalManagement.Areas.Patient.Models
{
    // Main ViewModel that contains all three sections
    public class BillingViewModel
    {
        public List<PendingDepositViewModel> PendingDeposits { get; set; } = new List<PendingDepositViewModel>();
        public List<OutstandingPaymentViewModel> OutstandingPayments { get; set; } = new List<OutstandingPaymentViewModel>();
        public List<PaymentHistoryViewModel> PaymentHistory { get; set; } = new List<PaymentHistoryViewModel>();
    }

    // ViewModel for appointments requiring deposit payment
    public class PendingDepositViewModel
    {
        public int AppointmentId { get; set; }
        public string TreatmentName { get; set; }
        public string DoctorName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string AppointmentStatus { get; set; }
    }

    // ViewModel for appointments with deposit paid but requiring final payment
    public class OutstandingPaymentViewModel
    {
        public int AppointmentId { get; set; }
        public string TreatmentName { get; set; }
        public string DoctorName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string AppointmentStatus { get; set; }
        public string DepositReceiptUrl { get; set; }
        public bool IsEligibleForPayment { get; set; } // Only completed appointments should be eligible
    }

    // ViewModel for payment history (fully paid and refunds)
    public class PaymentHistoryViewModel
    {
        public int AppointmentId { get; set; }
        public int PaymentId { get; set; }
        public string TreatmentName { get; set; }
        public string DoctorName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public PaymentType PaymentType { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ReceiptUrl { get; set; }
        public string AppointmentStatus { get; set; }
    }
}