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

        // Existing DbSets
        public DbSet<Patient> Patients { get; set; }
        public DbSet<TreatmentType> TreatmentTypes { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<DoctorTreatment> DoctorTreatments { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        // Add Appointment DbSet
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveAllocation> LeaveAllocations { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure DoctorTreatment (using Id as primary key instead of composite key)
            modelBuilder.Entity<DoctorTreatment>()
                .HasKey(dt => dt.Id);

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

            // Configure Appointment Relationships
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.TreatmentType)
                .WithMany()
                .HasForeignKey(a => a.TreatmentTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Appointment constraints
            modelBuilder.Entity<Appointment>()
                .Property(a => a.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Scheduled");

            modelBuilder.Entity<Appointment>()
                .Property(a => a.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                    
            // Configure Leave related DateTime fields with DateTimeKind.Utc
            modelBuilder.Entity<LeaveRequest>()
                .Property(lr => lr.StartDate)
                .HasConversion(
                    v => v, 
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
            modelBuilder.Entity<LeaveRequest>()
                .Property(lr => lr.EndDate)
                .HasConversion(
                    v => v, 
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
            modelBuilder.Entity<LeaveRequest>()
                .Property(lr => lr.DateRequested)
                .HasConversion(
                    v => v, 
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
            modelBuilder.Entity<LeaveRequest>()
                .Property(lr => lr.DateActioned)
                .HasConversion(
                    v => v.HasValue ? v.Value : DateTime.UtcNow, 
                    v => v == DateTime.MinValue ? (DateTime?)null : DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
            modelBuilder.Entity<LeaveAllocation>()
                .Property(la => la.DateCreated)
                .HasConversion(
                    v => v, 
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
            modelBuilder.Entity<LeaveAllocation>()
                .Property(la => la.DateModified)
                .HasConversion(
                    v => v, 
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
            // Configure nullable string properties for leave management
            modelBuilder.Entity<LeaveRequest>()
                .Property(lr => lr.RequestComments)
                .IsRequired(false);
                
            modelBuilder.Entity<LeaveRequest>()
                .Property(lr => lr.ApproverComments)
                .IsRequired(false);
                
            modelBuilder.Entity<LeaveRequest>()
                .Property(lr => lr.ApproverId)
                .IsRequired(false);
                
            modelBuilder.Entity<LeaveRequest>()
                .Property(lr => lr.DocumentPath)
                .IsRequired(false);
                
            modelBuilder.Entity<LeaveType>()
                .Property(lt => lt.Description)
                .IsRequired(false);
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