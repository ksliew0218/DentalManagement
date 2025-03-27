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
        
        // Related data for the logged-in doctor
        public DentalManagement.Models.Doctor? CurrentDoctor { get; set; }
        
        // Lists for recent data
        public List<TimeSlot> RecentTimeSlots { get; set; } = new List<TimeSlot>();
        
        // Can be expanded with more statistics as needed
    }
} 