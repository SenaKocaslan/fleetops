using FleetOps.Fleet.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Fleet.Persistence;

public sealed class FleetDbContext(DbContextOptions<FleetDbContext> options) : DbContext(options)
{
    public const string Schema = "fleet";

    public DbSet<Agv> Agvs => Set<Agv>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FleetDbContext).Assembly);
    }
}
