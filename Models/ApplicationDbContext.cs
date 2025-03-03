using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Models  // Ensure this matches your project namespace
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        // ✅ Add this line to ensure Patient model is found
        public DbSet<Patient> Patients { get; set; }
        public DbSet<TreatmentType> TreatmentTypes { get; set; }
    }
}