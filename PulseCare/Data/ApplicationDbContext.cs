using Microsoft.EntityFrameworkCore;
using PulseCare.Models;

namespace PulseCare.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Consultation> Consultations { get; set; }
        public DbSet<Reminder> Reminders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed mock accounts so you don't have to manually register them
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, FullName = "Dr. Smith", Role = "Doctor", Email = "doctor@pulsecare.com", Password = "123" },
                new User { Id = 2, FullName = "Adrian Kim", Role = "Patient", Email = "patient@pulsecare.com", Password = "123" }
            );
        }
    }
}