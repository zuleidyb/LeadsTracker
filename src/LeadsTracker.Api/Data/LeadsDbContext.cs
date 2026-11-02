using LeadsTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LeadsTracker.Api.Data;

public class LeadsDbContext(DbContextOptions<LeadsDbContext> options) : DbContext(options)
{
    public DbSet<Lead> Leads => Set<Lead>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lead>(lead =>
        {
            lead.Property(l => l.FirstName).HasMaxLength(100);
            lead.Property(l => l.LastName).HasMaxLength(100);
            lead.Property(l => l.Email).HasMaxLength(254);
            lead.Property(l => l.Company).HasMaxLength(200);
            lead.Property(l => l.Phone).HasMaxLength(30);
            lead.Property(l => l.JobTitle).HasMaxLength(150);
            lead.Property(l => l.Industry).HasMaxLength(100);
            lead.Property(l => l.Owner).HasMaxLength(150);
            lead.Property(l => l.EstimatedValue).HasPrecision(12, 2);
            lead.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);
            lead.Property(l => l.Source).HasConversion<string>().HasMaxLength(20);

            lead.HasIndex(l => l.Email).IsUnique();
            lead.HasIndex(l => l.Status);
            lead.HasIndex(l => l.Source);
            lead.HasIndex(l => l.CreatedAt);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyLeadRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyLeadRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyLeadRules()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Lead>())
        {
            if (entry.State is EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.Email = entry.Entity.Email.Trim().ToLowerInvariant();
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
