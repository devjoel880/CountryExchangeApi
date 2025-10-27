using Microsoft.EntityFrameworkCore;
using CountryExchangeApi.Models;

namespace CountryExchangeApi.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Country> Countries { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Index on Name for lookup; case-insensitive depend on DB collation
            modelBuilder.Entity<Country>()
                .HasIndex(c => c.Name);
        }
    }
}
