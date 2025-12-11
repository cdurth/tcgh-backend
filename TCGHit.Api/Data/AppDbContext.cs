using Microsoft.EntityFrameworkCore;
using TCGHit.Api.Models;

namespace TCGHit.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Subscriber> Subscribers => Set<Subscriber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Subscriber>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(254);

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.Source)
                .HasMaxLength(50);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45); // IPv6 max length

            entity.Property(e => e.SubscribedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });
    }
}
