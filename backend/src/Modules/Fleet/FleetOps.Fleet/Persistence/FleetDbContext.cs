using FleetOps.Fleet.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Fleet.Persistence;

// Fleet modulunun veritabani baglami. Yalnizca "fleet" semasini gorur;
// diger modullerin tablolarina erisemez.
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
