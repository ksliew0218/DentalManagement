using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DentalManagement.Models;

namespace DentalManagement.Models
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<TreatmentType> TreatmentTypes { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<DoctorTreatment> DoctorTreatments { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<DoctorLeaveBalance> DoctorLeaveBalances { get; set; }
        public DbSet<DoctorLeaveRequest> DoctorLeaveRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure DoctorTreatment composite key
            modelBuilder.Entity<DoctorTreatment>()
                .HasKey(dt => new { dt.DoctorId, dt.TreatmentTypeId });

            // Configure DoctorTreatment relationships
            modelBuilder.Entity<DoctorTreatment>()
                .HasOne(dt => dt.Doctor)
                .WithMany(d => d.DoctorTreatments)
                .HasForeignKey(dt => dt.DoctorId);

            modelBuilder.Entity<DoctorTreatment>()
                .HasOne(dt => dt.TreatmentType)
                .WithMany(t => t.DoctorTreatments)
                .HasForeignKey(dt => dt.TreatmentTypeId);
                
            // Configure TimeSlot with DateTimeKind.Utc for PostgreSQL
            modelBuilder.Entity<TimeSlot>()
                .Property(t => t.StartTime)
                .HasConversion(
                    v => v, 
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
            modelBuilder.Entity<TimeSlot>()
                .Property(t => t.EndTime)
                .HasConversion(
                    v => v, 
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
            // Configure DateTime properties for leave management
            modelBuilder.Entity<DoctorLeaveRequest>()
                .Property(l => l.StartDate)
                .HasConversion(
                    v => v, 
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
            modelBuilder.Entity<DoctorLeaveRequest>()
                .Property(l => l.EndDate)
                .HasConversion(
                    v => v, 
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
            modelBuilder.Entity<DoctorLeaveRequest>()
                .Property(l => l.RequestDate)
                .HasConversion(
                    v => v, 
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
            modelBuilder.Entity<DoctorLeaveRequest>()
                .Property(l => l.ApprovalDate)
                .HasConversion(
                    v => v.HasValue ? v.Value : DateTime.MinValue, 
                    v => v == DateTime.MinValue ? (DateTime?)null : DateTime.SpecifyKind(v, DateTimeKind.Utc));
        }

        // Ensure UTC DateTime values when saving to database
        public override int SaveChanges()
        {
            ProcessDateTimes();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ProcessDateTimes();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ProcessDateTimes()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    foreach (var property in entry.Entity.GetType().GetProperties())
                    {
                        if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                        {
                            var value = property.GetValue(entry.Entity);
                            if (value != null)
                            {
                                if (value is DateTime dateTime)
                                {
                                    // Always store DateTimes as UTC in PostgreSQL
                                    if (dateTime.Kind != DateTimeKind.Utc)
                                    {
                                        property.SetValue(entry.Entity, DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}