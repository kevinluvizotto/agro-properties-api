using AgroProperties.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgroProperties.Api.Data;

public class PropertiesDbContext : DbContext
{
    public PropertiesDbContext(DbContextOptions<PropertiesDbContext> options) : base(options) { }

    public DbSet<FarmProperty> Properties => Set<FarmProperty>();
    public DbSet<Plot> Plots => Set<Plot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1 DB único -> isola no schema "properties"
        modelBuilder.HasDefaultSchema("properties");

        modelBuilder.Entity<FarmProperty>()
            .HasMany(p => p.Plots)
            .WithOne(t => t.Property!)
            .HasForeignKey(t => t.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FarmProperty>()
            .HasIndex(p => new { p.ProducerId, p.Name });

        modelBuilder.Entity<Plot>()
            .HasIndex(t => new { t.PropertyId, t.Name });

        base.OnModelCreating(modelBuilder);
    }
}