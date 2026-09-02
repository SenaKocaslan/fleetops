using FleetOps.Tasks.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Tasks.Persistence;

// Tasks modulunun veritabani baglami. Yalnizca "tasks" semasini gorur.
public sealed class TasksDbContext(DbContextOptions<TasksDbContext> options) : DbContext(options)
{
    public const string Schema = "tasks";

    public DbSet<TransportTask> TransportTasks => Set<TransportTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TasksDbContext).Assembly);
    }
}
