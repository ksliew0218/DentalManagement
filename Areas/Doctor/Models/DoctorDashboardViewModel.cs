using System;
using System.Collections.Generic;
using DentalManagement.Models;

namespace DentalManagement.Areas.Doctor.Models
{
    public class DoctorDashboardViewModel
    {
        public int AppointmentCount { get; set; }
        public int PatientCount { get; set; }
        public int TreatmentCount { get; set; }
        public int UpcomingAppointments { get; set; }
        public int TimeSlotCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingLeaveRequests { get; set; }
        
        // Related data for the logged-in doctor
        public DentalManagement.Models.Doctor? CurrentDoctor { get; set; }
        
        // Lists for recent data
        public List<TimeSlot> RecentTimeSlots { get; set; } = new List<TimeSlot>();
        
        // New properties for enhanced dashboard
        public List<TimeSlot> TodayTimeSlots { get; set; } = new List<TimeSlot>();
        public List<TimeSlot> UpcomingTimeSlots { get; set; } = new List<TimeSlot>();
        public List<Appointment> TodayAppointments { get; set; } = new List<Appointment>();
        
        // Leave management data
        public List<DoctorLeaveBalance> LeaveBalances { get; set; } = new List<DoctorLeaveBalance>();
        public DoctorLeaveRequest? UpcomingLeave { get; set; }
        
        // Can be expanded with more statistics as needed
    }
} 