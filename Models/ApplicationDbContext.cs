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
        public DbSet<AppointmentReminder> AppointmentReminders { get; set; }
        public DbSet<UserNotificationPreferences> UserNotificationPreferences { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<TreatmentReport> TreatmentReports { get; set; }
        public DbSet<AppointmentDocument> AppointmentDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DoctorTreatment>()
                .HasKey(dt => new { dt.DoctorId, dt.TreatmentTypeId });

            
            modelBuilder.Entity<DoctorTreatment>()
                .HasOne(dt => dt.Doctor)
                .WithMany(d => d.DoctorTreatments)
                .HasForeignKey(dt => dt.DoctorId);

            modelBuilder.Entity<DoctorTreatment>()
                .HasOne(dt => dt.TreatmentType)
                .WithMany(t => t.DoctorTreatments)
                .HasForeignKey(dt => dt.TreatmentTypeId);
                
            
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
                    
            
            modelBuilder.Entity<TimeSlot>()
                .HasOne(ts => ts.Appointment)
                .WithMany(a => a.TimeSlots)
                .HasForeignKey(ts => ts.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            
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

            
            modelBuilder.Entity<Appointment>()
                .Property(a => a.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Scheduled");

            modelBuilder.Entity<Appointment>()
                .Property(a => a.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            
            modelBuilder.Entity<Appointment>()
                .Property(a => a.Duration)
                .HasDefaultValue(60);

            
            modelBuilder.Entity<Appointment>()
                .Property(a => a.PaymentStatus)
                .HasDefaultValue(PaymentStatus.Pending);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Appointment>()
                .Property(a => a.DepositAmount)
                .HasColumnType("decimal(18,2)");

            
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Appointment)
                .WithMany(a => a.Payments)
                .HasForeignKey(p => p.AppointmentId);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            modelBuilder.Entity<Payment>()
                .Property(p => p.Status)
                .HasMaxLength(50)
                .HasDefaultValue("pending");

            modelBuilder.Entity<Payment>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            
            modelBuilder.Entity<Payment>()
                .Property(p => p.CreatedAt)
                .HasConversion(
                    v => v, 
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
            modelBuilder.Entity<Payment>()
                .Property(p => p.UpdatedAt)
                .HasConversion(
                    v => v.HasValue ? v.Value : DateTime.MinValue, 
                    v => v == DateTime.MinValue ? (DateTime?)null : DateTime.SpecifyKind(v, DateTimeKind.Utc));

            
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

           
            modelBuilder.Entity<UserNotification>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasConversion(
                        v => v, 
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
                entity.Property(e => e.ReadAt)
                    .HasConversion(
                        v => v.HasValue ? v.Value : DateTime.MinValue, 
                        v => v == DateTime.MinValue ? (DateTime?)null : DateTime.SpecifyKind(v, DateTimeKind.Utc));
                        
                entity.Property(e => e.EmailSentAt)
                    .HasConversion(
                        v => v.HasValue ? v.Value : DateTime.MinValue, 
                        v => v == DateTime.MinValue ? (DateTime?)null : DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);
                    
                entity.Property(e => e.Message)
                    .IsRequired();
                    
                entity.Property(e => e.NotificationType)
                    .IsRequired()
                    .HasMaxLength(50);
            });
            
            modelBuilder.Entity<UserNotificationPreferences>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.Property(e => e.LastUpdated)
                    .IsRequired()
                    .HasConversion(
                        v => v, 
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            });

            
            modelBuilder.Entity<AppointmentReminder>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Appointment)
                    .WithMany(a => a.Reminders)
                    .HasForeignKey(e => e.AppointmentId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.Property(e => e.SentAt)
                    .IsRequired()
                    .HasConversion(
                        v => v, 
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
                entity.Property(e => e.ReminderType)
                    .IsRequired()
                    .HasMaxLength(50);
            });
            
            
            modelBuilder.Entity<TreatmentReport>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Appointment)
                    .WithMany(a => a.TreatmentReports)
                    .HasForeignKey(e => e.AppointmentId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Doctor)
                    .WithMany()
                    .HasForeignKey(e => e.DoctorId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Patient)
                    .WithMany()
                    .HasForeignKey(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasConversion(
                        v => v, 
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                    
                entity.Property(e => e.UpdatedAt)
                    .HasConversion(
                        v => v.HasValue ? v.Value : DateTime.MinValue, 
                        v => v == DateTime.MinValue ? (DateTime?)null : DateTime.SpecifyKind(v, DateTimeKind.Utc));
                        
                entity.Property(e => e.TreatmentDate)
                    .IsRequired()
                    .HasConversion(
                        v => v, 
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                
                entity.Property(e => e.DentalChart)
                    .HasColumnType("text");
            });
        }

        
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