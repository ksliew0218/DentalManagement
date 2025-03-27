using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Services
{
    public class LeaveManagementService
    {
        private readonly ApplicationDbContext _context;

        public LeaveManagementService(ApplicationDbContext context)
        {
            _context = context;
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
            // Calculate business days
            request.TotalDays = CalculateBusinessDays(request.StartDate, request.EndDate);
            
            // Validate dates
            if (request.EndDate < request.StartDate)
            {
                return (false, "End date cannot be before start date.");
            }
            
            if (request.StartDate < DateTime.UtcNow.Date)
            {
                return (false, "Cannot apply for leave in the past.");
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
                    return (false, "Leave balance not found.");
                }
                
                if (leaveBalance.RemainingDays < request.TotalDays)
                {
                    return (false, $"Insufficient leave balance. Available: {leaveBalance.RemainingDays} days.");
                }
            }
            
            // Add the request
            _context.DoctorLeaveRequests.Add(request);
            await _context.SaveChangesAsync();
            
            return (true, "Leave request submitted successfully.");
        }
        
        // Approve or reject a leave request
        public async Task<(bool Success, string Message)> UpdateLeaveRequestStatusAsync(
            int requestId, LeaveRequestStatus status, string approvedById, string comments = null)
        {
            var request = await _context.DoctorLeaveRequests
                .Include(lr => lr.LeaveType)
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
            
            // If approved and paid, deduct from leave balance
            if (status == LeaveRequestStatus.Approved && request.LeaveType.IsPaid)
            {
                var leaveBalance = await _context.DoctorLeaveBalances
                    .FirstOrDefaultAsync(lb => lb.DoctorId == request.DoctorId && 
                                             lb.LeaveTypeId == request.LeaveTypeId &&
                                             lb.Year == DateTime.UtcNow.Year);
                                             
                if (leaveBalance == null)
                {
                    return (false, "Leave balance not found.");
                }
                
                if (leaveBalance.RemainingDays < request.TotalDays)
                {
                    return (false, $"Insufficient leave balance. Available: {leaveBalance.RemainingDays} days.");
                }
                
                leaveBalance.RemainingDays -= request.TotalDays;
            }
            
            await _context.SaveChangesAsync();
            
            return (true, $"Leave request {status.ToString().ToLower()} successfully.");
        }
        
        // Get doctor leave balance summary
        public async Task<List<DoctorLeaveBalance>> GetDoctorLeaveBalancesAsync(int doctorId, int year)
        {
            return await _context.DoctorLeaveBalances
                .Include(lb => lb.LeaveType)
                .Where(lb => lb.DoctorId == doctorId && lb.Year == year)
                .ToListAsync();
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