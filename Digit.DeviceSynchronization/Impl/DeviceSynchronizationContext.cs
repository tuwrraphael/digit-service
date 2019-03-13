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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StoredDevice>().HasKey(v => v.Id);
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
}
