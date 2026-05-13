using Microsoft.EntityFrameworkCore;

namespace StripeOnboardingSlice.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<ProcessedWebhook> ProcessedWebhooks => Set<ProcessedWebhook>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedWebhook>()
            .HasKey(w => w.EventId);

        modelBuilder.Entity<Company>()
            .Property(c => c.Status)
            .HasConversion<string>();
    }
}