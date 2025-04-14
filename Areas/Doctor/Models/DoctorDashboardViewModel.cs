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
        
        public DentalManagement.Models.Doctor? CurrentDoctor { get; set; }
        
        public List<TimeSlot> RecentTimeSlots { get; set; } = new List<TimeSlot>();
        
        public List<TimeSlot> TodayTimeSlots { get; set; } = new List<TimeSlot>();
        public List<TimeSlot> UpcomingTimeSlots { get; set; } = new List<TimeSlot>();
        public List<Appointment> TodayAppointments { get; set; } = new List<Appointment>();
        
        public List<DoctorLeaveBalance> LeaveBalances { get; set; } = new List<DoctorLeaveBalance>();
        public DoctorLeaveRequest? UpcomingLeave { get; set; }
        
    }
} 