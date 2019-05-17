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
        public DbSet<StoredFocusItem> FocusItems { get; set; }
        public DbSet<StoredGeoFence> Geofences { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(p => p.Id);
            modelBuilder.Entity<User>()
                .HasOne(v => v.StoredLocation)
                .WithOne(v => v.User)
                .HasForeignKey<User>(v => v.StoredLocationId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<User>()
                .HasMany(v => v.FocusItems)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoredLocation>()
                .HasKey(p => p.Id);
            modelBuilder.Entity<StoredLocation>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<StoredFocusItem>()
                .HasKey(v => v.Id);
            modelBuilder.Entity<StoredFocusItem>()
                .HasOne(v => v.CalendarEvent)
                .WithOne(v => v.FocusItem)
                .HasForeignKey<StoredFocusItem>(v => new { v.CalendarEventId, v.CalendarEventFeedId })
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoredFocusItem>()
               .HasOne(v => v.Directions)
               .WithOne(v => v.FocusItem)
               .HasForeignKey<StoredDirectionsInfo>(v => v.FocusItemId)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoredCalendarEvent>()
                .HasKey(v => new { v.Id, v.FeedId });

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

            modelBuilder.Entity<StoredDirectionsInfo>()
                .HasKey(d => d.FocusItemId);

            modelBuilder.Entity<StoredGeoFence>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<StoredGeoFence>()
                .HasOne(v => v.FocusItem)
                .WithMany(v => v.Geofences)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}