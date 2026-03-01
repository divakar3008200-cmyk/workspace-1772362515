using Microsoft.EntityFrameworkCore;
using dotnetapp.Models;

namespace dotnetapp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Aircraft> Aircrafts { get; set; } = null!;
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Aircraft>()
                .HasMany(a => a.MaintenanceRecords)
                .WithOne(m => m.Aircraft)
                .HasForeignKey(m => m.AircraftId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}