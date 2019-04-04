using Microsoft.EntityFrameworkCore;
using System;

namespace Digit.DeviceSynchronization.Impl
{
    public class DeviceSynchronizationContext : DbContext
    {
        public DeviceSynchronizationContext(DbContextOptions<DeviceSynchronizationContext> contextOptions) : base(contextOptions)
        {

        }

        internal DbSet<StoredDevice> Devices { get; set; }
        internal DbSet<StoredSyncAction> SyncActions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StoredDevice>().HasKey(v => v.Id);
            modelBuilder.Entity<StoredDevice>().ToTable("Sync_Devices");
            modelBuilder.Entity<StoredDevice>().Property(v => v.OwnerId).IsRequired();
            modelBuilder.Entity<StoredSyncAction>()
                .HasKey(v => v.Id);
            modelBuilder.Entity<StoredSyncAction>().ToTable("Sync_SyncActions");
            modelBuilder.Entity<StoredSyncAction>().Property(v => v.ActionId).IsRequired();
            modelBuilder.Entity<StoredSyncAction>().Property(v => v.RequestedFor).IsRequired();
            modelBuilder.Entity<StoredSyncAction>().Property(v => v.UserId).IsRequired();
        }
    }

    internal class StoredDevice
    {
        public string Id { get; set; }
        public string OwnerId { get; set; }
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
