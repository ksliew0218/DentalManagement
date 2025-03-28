using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DentalManagement.Areas.Patient.Models
{
    // Main view model for appointments list
    public class AppointmentsListViewModel
    {
        public List<AppointmentViewModel> UpcomingAppointments { get; set; }
        public List<AppointmentViewModel> PastAppointments { get; set; }
    }

    // Individual appointment view model
    public class AppointmentViewModel
    {
        public int Id { get; set; }
        public string TreatmentName { get; set; }
        public string DoctorName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; }
        public bool CanCancel { get; set; }
        
        // Formatted date and time properties for display
        public string FormattedDate => AppointmentDate.ToString("MMM d, yyyy");
        public string FormattedTime 
        { 
            get 
            {
                try
                {
                    bool isPM = AppointmentTime.Hours >= 12;
                    int hour12 = AppointmentTime.Hours % 12;
                    if (hour12 == 0) hour12 = 12;
                    return $"{hour12}:{AppointmentTime.Minutes:D2} {(isPM ? "PM" : "AM")}";
                }
                catch (Exception)
                {
                    // Return a default or placeholder if there's an issue
                    return "Time not available";
                }
            }
        }
        
        // CSS class based on status
        public string StatusClass
        {
            get
            {
                try
                {
                    return Status?.ToLower() switch
                    {
                        "scheduled" => "scheduled",
                        "confirmed" => "confirmed",
                        "completed" => "completed",
                        "cancelled" => "cancelled",
                        "no-show" => "no-show",
                        _ => "pending"
                    };
                }
                catch
                {
                    return "pending";
                }
            }
        }
    }

    // View model for booking an appointment
    public class AppointmentBookingViewModel
    {
        [Required]
        public int TreatmentId { get; set; }
        public string TreatmentName { get; set; }
        public string TreatmentDuration { get; set; }
        public string TreatmentPrice { get; set; }
        
        [Required]
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string DoctorSpecialization { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }
        
        [Required]
        public string AppointmentTime { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; }
    }
    
    // Detailed appointment view
    public class AppointmentDetailViewModel
    {
        public int Id { get; set; }
        public string TreatmentName { get; set; }
        public string DoctorName { get; set; }
        public string DoctorSpecialization { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public bool CanCancel { get; set; }
        
        // Add the missing CreatedOn property
        public DateTime CreatedOn { get; set; }
        
        // Additional info that might be useful
        public decimal TreatmentCost { get; set; }
        public int TreatmentDuration { get; set; }
        public string FormattedAppointmentDate => AppointmentDate.ToString("dddd, MMMM d, yyyy");
        public string FormattedAppointmentTime 
        { 
            get 
            {
                try
                {
                    bool isPM = AppointmentTime.Hours >= 12;
                    int hour12 = AppointmentTime.Hours % 12;
                    if (hour12 == 0) hour12 = 12;
                    return $"{hour12}:{AppointmentTime.Minutes:D2} {(isPM ? "PM" : "AM")}";
                }
                catch (Exception)
                {
                    // Return a default or placeholder if there's an issue
                    return "Time not available";
                }
            }
        }
        
        // CSS class based on status
        public string StatusClass
        {
            get
            {
                try
                {
                    return Status?.ToLower() switch
                    {
                        "scheduled" => "scheduled",
                        "confirmed" => "confirmed",
                        "completed" => "completed",
                        "cancelled" => "cancelled",
                        "no-show" => "no-show",
                        _ => "pending"
                    };
                }
                catch
                {
                    return "pending";
                }
            }
        }
    }

    public class TreatmentViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public string ImageUrl { get; set; }
        
        // We'll determine this based on the name since your model doesn't have a Category field
        public string Category => DetermineCategory(Name);
        
        // Formatted price and duration for display
        public string FormattedPrice => $"RM {Price:F2}";
        public string FormattedDuration => $"{Duration} min";
        
        private string DetermineCategory(string name)
        {
            name = name?.ToLower() ?? "";
            
            if (name.Contains("cleaning") || name.Contains("scaling") || name.Contains("polish"))
                return "Cleaning";
                
            if (name.Contains("whitening") || name.Contains("veneer") || name.Contains("cosmetic"))
                return "Cosmetic";
                
            if (name.Contains("filling") || name.Contains("crown") || name.Contains("root canal") || 
                name.Contains("restoration"))
                return "Restorative";
                
            if (name.Contains("extraction") || name.Contains("surgery") || name.Contains("implant"))
                return "Surgical";
                
            if (name.Contains("braces") || name.Contains("aligner") || name.Contains("orthodontic"))
                return "Orthodontic";
                
            return "Other";
        }
    }

    public class TreatmentSelectionViewModel
    {
        public List<string> Categories { get; set; }
        public List<TreatmentViewModel> Treatments { get; set; }
    }
    
    // View model for doctor selection
    public class DoctorViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Specialization { get; set; }
        public int YearsOfExperience { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Qualifications { get; set; }
        
        // Formatted experience for display
        public string FormattedExperience => $"{YearsOfExperience} years experience";
    }

    public class TimeSlotViewModel
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string FormattedStartTime { get; set; }
        public string Period { get; set; }
        
        // New properties for multi-hour appointments
        public int RequiredSlots { get; set; } = 1;
        public List<int> ConsecutiveSlotIds { get; set; } = new List<int>();

            // Display duration
        public string FormattedDuration => RequiredSlots == 1 
            ? "1 hour" 
            : $"{RequiredSlots} hours";
    }
    }