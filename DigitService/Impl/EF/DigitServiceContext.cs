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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(p => p.Id);
        }
    }
}