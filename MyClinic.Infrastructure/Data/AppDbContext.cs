using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyClinic.Domain.Entities;

namespace MyClinic.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<SlotConfig> SlotConfigs => Set<SlotConfig>();
        public DbSet<Availability> Availabilities => Set<Availability>();
        public DbSet<Appointment> Appointments => Set<Appointment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Availability -> Doctor relationship
            modelBuilder.Entity<Availability>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Appointment -> Doctor relationship
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Appointment -> Patient (User) relationship
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting user if they have appointments

            // Configure SlotConfig as singleton (optional index for uniqueness)
            modelBuilder.Entity<SlotConfig>()
                .HasIndex(s => s.Id)
                .IsUnique();
        }
    }
}
