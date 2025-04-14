using System;
using System.Collections.Generic;
using DentalManagement.Models;

namespace DentalManagement.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int PatientCount { get; set; }
        public int DoctorCount { get; set; }
        public int TreatmentTypeCount { get; set; }
        public int AppointmentCount { get; set; }
        public decimal TotalRevenue { get; set; }
        
    }
} 