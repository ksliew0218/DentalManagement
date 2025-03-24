using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
    }
}