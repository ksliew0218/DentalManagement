using System;
using System.Collections.Generic;
using DentalManagement.Models;

namespace DentalManagement.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalScheduledAppointments { get; set; }
        public int TotalCompletedAppointments { get; set; }
        public int TotalCancelledAppointments { get; set; }
        public List<Appointment> TodayAppointments { get; set; } = new List<Appointment>();
        
        // Doctor specific stats
        public Dictionary<int, int> AppointmentsPerDoctor { get; set; } = new Dictionary<int, int>();
        
        // Treatment specific stats
        public Dictionary<int, int> AppointmentsPerTreatment { get; set; } = new Dictionary<int, int>();
    }
} 