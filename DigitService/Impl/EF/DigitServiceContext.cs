using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Kontokorrent.Impl.EF
{
    public class DigitServiceContext : DbContext
    {
        public DigitServiceContext(DbContextOptions<DigitServiceContext> contextOptions) : base(contextOptions)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Device> Devices { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(p => p.Id);
            modelBuilder.Entity<User>()
                .HasMany(p => p.Devices)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId);

            modelBuilder.Entity<Device>()
                .HasKey(p => p.Id);
        }
    }

    public class Device
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
    }

    public class User
    {
        public string Id { get; set; }
        public string PushChannel { get; set; }
        public List<Device> Devices { get; set; }
    }
}