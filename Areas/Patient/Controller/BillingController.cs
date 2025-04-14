using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DentalManagement.Areas.Patient.Models;
using DentalManagement.Models;
using Microsoft.AspNetCore.Identity;

namespace DentalManagement.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize]
    public class BillingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public BillingController(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserID == user.Id);

            if (patient == null)
            {
                return NotFound("Patient profile not found.");
            }

            var viewModel = new BillingViewModel();

            var pendingDeposits = await _context.Appointments
                .Include(a => a.TreatmentType)
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patient.Id && 
                            a.PaymentStatus == PaymentStatus.Pending &&
                            a.Status != "Cancelled" && 
                            a.Status != "Completed")
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync();

            foreach (var appointment in pendingDeposits)
            {
                viewModel.PendingDeposits.Add(new PendingDepositViewModel
                {
                    AppointmentId = appointment.Id,
                    TreatmentName = appointment.TreatmentType?.Name,
                    DoctorName = $"Dr. {appointment.Doctor?.FirstName} {appointment.Doctor?.LastName}",
                    AppointmentDate = appointment.AppointmentDate,
                    AppointmentTime = appointment.AppointmentTime,
                    TotalAmount = appointment.TotalAmount,
                    DepositAmount = appointment.DepositAmount,
                    PaymentStatus = appointment.PaymentStatus,
                    AppointmentStatus = appointment.Status
                });
            }

            var outstandingPayments = await _context.Appointments
                .Include(a => a.TreatmentType)
                .Include(a => a.Doctor)
                .Include(a => a.Payments)
                .Where(a => a.PatientId == patient.Id && 
                            a.PaymentStatus == PaymentStatus.PartiallyPaid &&
                            a.Status == "Completed")
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync();

            foreach (var appointment in outstandingPayments)
            {
                var depositPayment = appointment.Payments
                    .FirstOrDefault(p => p.PaymentType == PaymentType.Deposit && p.Status == "succeeded");

                viewModel.OutstandingPayments.Add(new OutstandingPaymentViewModel
                {
                    AppointmentId = appointment.Id,
                    TreatmentName = appointment.TreatmentType?.Name,
                    DoctorName = $"Dr. {appointment.Doctor?.FirstName} {appointment.Doctor?.LastName}",
                    AppointmentDate = appointment.AppointmentDate,
                    AppointmentTime = appointment.AppointmentTime,
                    TotalAmount = appointment.TotalAmount,
                    DepositAmount = appointment.DepositAmount,
                    RemainingAmount = appointment.TotalAmount - appointment.DepositAmount,
                    PaymentStatus = appointment.PaymentStatus,
                    AppointmentStatus = appointment.Status,
                    DepositReceiptUrl = depositPayment?.ReceiptUrl
                });
            }

            var paymentHistory = await _context.Payments
                .Include(p => p.Appointment)
                .ThenInclude(a => a.TreatmentType)
                .Include(p => p.Appointment)
                .ThenInclude(a => a.Doctor)
                .Where(p => p.Appointment.PatientId == patient.Id &&
                           (p.Status == "succeeded" || p.PaymentType == PaymentType.Refund))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            foreach (var payment in paymentHistory)
            {
                viewModel.PaymentHistory.Add(new PaymentHistoryViewModel
                {
                    AppointmentId = payment.AppointmentId,
                    PaymentId = payment.Id,
                    TreatmentName = payment.Appointment?.TreatmentType?.Name,
                    DoctorName = $"Dr. {payment.Appointment?.Doctor?.FirstName} {payment.Appointment?.Doctor?.LastName}",
                    AppointmentDate = payment.Appointment?.AppointmentDate ?? DateTime.MinValue,
                    AppointmentTime = payment.Appointment?.AppointmentTime ?? TimeSpan.Zero,
                    Amount = payment.Amount,
                    Status = payment.Status,
                    PaymentType = payment.PaymentType,
                    PaymentDate = payment.CreatedAt,
                    ReceiptUrl = payment.ReceiptUrl,
                    AppointmentStatus = payment.Appointment?.Status
                });
            }

            return View(viewModel);
        }
    }
}