using Microsoft.EntityFrameworkCore;
using System;

namespace Digit.DeviceSynchronization.Impl
{
    internal class DeviceSynchronizationContext : DbContext
    {
        public DeviceSynchronizationContext(DbContextOptions<DeviceSynchronizationContext> contextOptions) : base(contextOptions)
        {

        }

        public DbSet<StoredDevice> Devices { get; set; }
        public DbSet<StoredSyncAction> SyncActions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StoredDevice>().HasKey(v => v.Id);
            modelBuilder.Entity<StoredSyncAction>()
                .HasKey(v => v.Id);
            modelBuilder.Entity<StoredSyncAction>().Property(v => v.ActionId).IsRequired();
            modelBuilder.Entity<StoredSyncAction>().Property(v => v.RequestedFor).IsRequired();
            modelBuilder.Entity<StoredSyncAction>().Property(v => v.UserId).IsRequired();
        }
    }

    internal class StoredDevice
    {
        public string Id { get; set; }
        public string OwnerId { get; set; }
        public bool UpToDate { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public string FocusItemId { get; set; }
        public string FocusItemDigest { get; set; }
    }

    internal class StoredSyncAction
    {
        public string Id { get; set; }
        public string ActionId { get; set; }
        public string UserId { get; set; }
        public DateTime RequestedFor { get; set; }
        public bool Done { get; set; }
    }
}
