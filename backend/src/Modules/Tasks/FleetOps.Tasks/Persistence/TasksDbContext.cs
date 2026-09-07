using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Tasks.Persistence;

public sealed class TasksDbContext(DbContextOptions<TasksDbContext> options) : DbContext(options)
{
    public const string Schema = "tasks";

    public DbSet<TransportTask> TransportTasks => Set<TransportTask>();

    public DbSet<Resource> Resources => Set<Resource>();

    public DbSet<ResourceLock> ResourceLocks => Set<ResourceLock>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // base.SaveChangesAsync'ten ONCE cagrilmali: outbox satiri durum
        // degisikligiyle ayni transaction'a boyle giriyor.
        OutboxaYaz();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void OutboxaYaz()
    {
        var aggregateler = ChangeTracker.Entries<AggregateRoot>()
            .Select(kayit => kayit.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregateler)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                var integrationEvent = IntegrationEventFactory.Olustur(domainEvent);
                if (integrationEvent is not null)
                {
                    OutboxMessages.Add(OutboxMessage.Olustur(integrationEvent));
                }
            }

            // Temizlenmezse ayni olay sonraki SaveChanges'te tekrar yazilir.
            aggregate.ClearDomainEvents();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TasksDbContext).Assembly);
    }
}
