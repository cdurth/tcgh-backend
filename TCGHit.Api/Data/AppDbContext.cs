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

            // Don't use HasDefaultValue/HasDefaultValueSql - let EF Core send values explicitly
            // The admin database may not have these default constraints
        });
    }
}
