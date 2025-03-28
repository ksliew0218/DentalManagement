using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalManagement.Services
{
    public class LeaveManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LeaveManagementService> _logger;

        public LeaveManagementService(ApplicationDbContext context, ILogger<LeaveManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Initialize leave balances for a new doctor
        public async Task InitializeDoctorLeaveBalancesAsync(int doctorId)
        {
            var leaveTypes = await _context.LeaveTypes.ToListAsync();
            var currentYear = DateTime.UtcNow.Year;
            
            var leaveBalances = new List<DoctorLeaveBalance>();
            
            foreach (var leaveType in leaveTypes)
            {
                leaveBalances.Add(new DoctorLeaveBalance
                {
                    DoctorId = doctorId,
                    LeaveTypeId = leaveType.Id,
                    RemainingDays = leaveType.DefaultDays,
                    TotalDays = leaveType.DefaultDays,
                    Year = currentYear
                });
            }
            
            _context.DoctorLeaveBalances.AddRange(leaveBalances);
            await _context.SaveChangesAsync();
        }
        
        // Calculate business days between two dates (excluding weekends)
        public decimal CalculateBusinessDays(DateTime startDate, DateTime endDate)
        {
            decimal businessDays = 0;
            DateTime currentDate = startDate.Date;
            
            while (currentDate <= endDate.Date)
            {
                // Check if the current day is not a weekend
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    businessDays++;
                }
                
                currentDate = currentDate.AddDays(1);
            }
            
            return businessDays;
        }
        
        // Process a leave request
        public async Task<(bool Success, string Message)> ProcessLeaveRequestAsync(DoctorLeaveRequest request)
        {
            try
            {
                // Calculate business days
                decimal businessDays = CalculateBusinessDays(request.StartDate, request.EndDate);
                request.TotalDays = businessDays;
                
                _logger.LogInformation($"Leave request: Start={request.StartDate:yyyy-MM-dd}, End={request.EndDate:yyyy-MM-dd}, Total Days={request.TotalDays}");
                
                // Validate dates
                if (request.EndDate < request.StartDate)
                {
                    return (false, "End date cannot be before start date.");
                }
                
                if (request.StartDate < DateTime.UtcNow.Date)
                {
                    return (false, "Cannot apply for leave in the past.");
                }
                
                if (businessDays <= 0)
                {
                    return (false, "Leave request must include at least one working day.");
                }
                
                // For paid leave types, check if doctor has enough balance
                var leaveType = await _context.LeaveTypes.FindAsync(request.LeaveTypeId);
                
                if (leaveType == null)
                {
                    return (false, "Invalid leave type.");
                }
                
                if (leaveType.IsPaid)
                {
                    var leaveBalance = await _context.DoctorLeaveBalances
                        .FirstOrDefaultAsync(lb => lb.DoctorId == request.DoctorId && 
                                                  lb.LeaveTypeId == request.LeaveTypeId &&
                                                  lb.Year == DateTime.UtcNow.Year);
                    
                    if (leaveBalance == null)
                    {
                        _logger.LogWarning($"Leave balance not found for doctor ID: {request.DoctorId}, leave type: {request.LeaveTypeId}");
                        return (false, "Leave balance not found. Please contact an administrator.");
                    }
                    
                    _logger.LogInformation($"Leave balance: Available={leaveBalance.RemainingDays}, Requested={request.TotalDays}");
                    
                    if (leaveBalance.RemainingDays < request.TotalDays)
                    {
                        return (false, $"Insufficient leave balance. Available: {leaveBalance.RemainingDays} days, Requested: {request.TotalDays} days.");
                    }
                }
                
                // Add the request
                await _context.DoctorLeaveRequests.AddAsync(request);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Leave request created successfully. ID: {request.Id}");
                
                return (true, "Leave request submitted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing leave request");
                return (false, "An error occurred while processing your leave request. Please try again.");
            }
        }
        
        // Process the rejection of a leave request
        private async Task HandleLeaveRequestRejectionAsync(DoctorLeaveRequest request)
        {
            try
            {
                // For rejected requests, we don't need to update the balance
                // But we might want to notify the doctor or perform other actions
                
                // Log the rejection
                _logger.LogInformation($"Leave request ID {request.Id} has been rejected. Doctor ID: {request.DoctorId}");
                
                // We could add code here to send a notification to the doctor
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling leave request rejection for ID: {request.Id}");
                // We don't throw the exception here to avoid disrupting the main workflow
            }
        }
        
        // Approve or reject a leave request
        public async Task<(bool Success, string Message)> UpdateLeaveRequestStatusAsync(
            int requestId, LeaveRequestStatus status, string approvedById, string? comments = null)
        {
            try
            {
                var request = await _context.DoctorLeaveRequests
                    .Include(lr => lr.LeaveType)
                    .Include(lr => lr.Doctor)
                    .FirstOrDefaultAsync(lr => lr.Id == requestId);
                    
                if (request == null)
                {
                    return (false, "Leave request not found.");
                }
                
                if (request.Status != LeaveRequestStatus.Pending)
                {
                    return (false, "Can only update pending leave requests.");
                }
                
                // Update status
                request.Status = status;
                request.ApprovedById = approvedById;
                request.ApprovalDate = DateTime.UtcNow;
                request.Comments = comments;
                
                // Handle based on status
                if (status == LeaveRequestStatus.Approved)
                {
                    // Handle leave balance update for paid leave
                    if (request.LeaveType.IsPaid)
                    {
                        var leaveBalance = await _context.DoctorLeaveBalances
                            .FirstOrDefaultAsync(lb => lb.DoctorId == request.DoctorId && 
                                                    lb.LeaveTypeId == request.LeaveTypeId &&
                                                    lb.Year == DateTime.UtcNow.Year);
                                                    
                        if (leaveBalance == null)
                        {
                            _logger.LogWarning($"Leave balance not found for request ID: {requestId}");
                            return (false, "Leave balance not found.");
                        }
                        
                        if (leaveBalance.RemainingDays < request.TotalDays)
                        {
                            return (false, $"Insufficient leave balance. Available: {leaveBalance.RemainingDays} days.");
                        }
                        
                        leaveBalance.RemainingDays -= request.TotalDays;
                        _logger.LogInformation($"Deducted {request.TotalDays} days from leave balance. New balance: {leaveBalance.RemainingDays}");
                    }
                    
                    // Handle time slots - mark as unavailable during leave period
                    await UpdateTimeSlotsDuringLeaveAsync(request);
                }
                else if (status == LeaveRequestStatus.Rejected)
                {
                    // Handle rejection specific operations
                    await HandleLeaveRequestRejectionAsync(request);
                }
                
                await _context.SaveChangesAsync();
                
                return (true, $"Leave request {status.ToString().ToLower()} successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating leave request status. ID: {requestId}");
                return (false, "An error occurred while updating the leave request.");
            }
        }
        
        // Helper method to mark time slots as unavailable during the leave period
        private async Task UpdateTimeSlotsDuringLeaveAsync(DoctorLeaveRequest leaveRequest)
        {
            try
            {
                // Get all time slots that fall within the leave period for this doctor
                var timeSlots = await _context.TimeSlots
                    .Where(ts => ts.DoctorId == leaveRequest.DoctorId &&
                               ts.StartTime.Date >= leaveRequest.StartDate.Date &&
                               ts.StartTime.Date <= leaveRequest.EndDate.Date)
                    .ToListAsync();
                    
                if (timeSlots.Any())
                {
                    _logger.LogInformation($"Found {timeSlots.Count} time slots for doctor ID {leaveRequest.DoctorId} during leave period");
                    
                    foreach (var slot in timeSlots)
                    {
                        // If the slot is already booked for an appointment, we should handle
                        // this carefully - we might want to reschedule or notify admin
                        if (slot.IsBooked)
                        {
                            _logger.LogWarning($"Time slot ID {slot.Id} is already booked during the approved leave period");
                            // Here we could add code to notify the admin or reschedule
                        }
                        
                        // Mark the slot as booked/unavailable during leave
                        slot.IsBooked = true;
                    }
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Updated {timeSlots.Count} time slots to unavailable during leave period");
                }
                else
                {
                    _logger.LogInformation($"No time slots found for doctor ID {leaveRequest.DoctorId} during leave period");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating time slots for leave request ID: {leaveRequest.Id}");
                // We don't throw here - we just log the error but still allow the leave approval to proceed
            }
        }
        
        // Get doctor leave balance summary
        public async Task<List<DoctorLeaveBalance>> GetDoctorLeaveBalancesAsync(int doctorId, int year)
        {
            var balances = await _context.DoctorLeaveBalances
                .Include(lb => lb.LeaveType)
                .Where(lb => lb.DoctorId == doctorId && lb.Year == year)
                .ToListAsync();
                
            // Fix any balances with TotalDays = 0
            foreach (var balance in balances)
            {
                if (balance.TotalDays == 0)
                {
                    balance.TotalDays = balance.RemainingDays > 0 ? balance.RemainingDays : 1;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Fixed zero TotalDays for leave balance ID {balance.Id}");
                }
            }
                
            return balances;
        }
        
        // Get doctor leave requests
        public async Task<List<DoctorLeaveRequest>> GetDoctorLeaveRequestsAsync(int doctorId)
        {
            return await _context.DoctorLeaveRequests
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.ApprovedByUser)
                .Where(lr => lr.DoctorId == doctorId)
                .OrderByDescending(lr => lr.RequestDate)
                .ToListAsync();
        }
        
        // Get all pending leave requests (for admin)
        public async Task<List<DoctorLeaveRequest>> GetPendingLeaveRequestsAsync()
        {
            return await _context.DoctorLeaveRequests
                .Include(lr => lr.Doctor)
                    .ThenInclude(d => d.User)
                .Include(lr => lr.LeaveType)
                .Where(lr => lr.Status == LeaveRequestStatus.Pending)
                .OrderBy(lr => lr.StartDate)
                .ToListAsync();
        }
    }
} 