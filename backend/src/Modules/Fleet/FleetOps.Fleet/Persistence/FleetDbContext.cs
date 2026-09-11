using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Fleet.Persistence;

public sealed class FleetDbContext(DbContextOptions<FleetDbContext> options) : DbContext(options)
{
    public const string Schema = "fleet";

    public DbSet<Agv> Agvs => Set<Agv>();

    public DbSet<ProcessedIntegrationEvent> ProcessedEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FleetDbContext).Assembly);
    }
}
