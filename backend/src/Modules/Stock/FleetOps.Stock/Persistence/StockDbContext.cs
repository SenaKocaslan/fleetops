using FleetOps.Stock.Domain;
using FleetOps.Stock.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Stock.Persistence;

// Stock modulunun veritabani baglami. Yalnizca "stock" semasini gorur.
public sealed class StockDbContext(DbContextOptions<StockDbContext> options) : DbContext(options)
{
    public const string Schema = "stock";

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<ProcessedIntegrationEvent> ProcessedEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StockDbContext).Assembly);
    }
}
