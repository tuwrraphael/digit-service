using DigitService.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitService.Impl.EF
{
    public class DigitServiceContext : DbContext
    {
        public DigitServiceContext(DbContextOptions<DigitServiceContext> contextOptions) : base(contextOptions)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<StoredBatteryMeasurement> BatteryMeasurements { get; set; }
        public DbSet<StoredDevice> Devices { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<StoredDevice>()
                .HasKey(p => p.Id);
            modelBuilder.Entity<StoredDevice>()
                .HasOne(p => p.User)
                .WithMany(p => p.Devices)
                .HasForeignKey(v => v.UserId);
            modelBuilder.Entity<StoredDevice>()
                .HasMany(p => p.BatteryMeasurements)
                .WithOne(v => v.Device)
                .HasForeignKey(v => v.DeviceId);

            modelBuilder.Entity<StoredBatteryMeasurement>()
                .HasKey(p => p.Id);
        }
    }
}