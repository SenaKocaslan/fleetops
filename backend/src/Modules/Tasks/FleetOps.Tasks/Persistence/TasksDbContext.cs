using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Tasks.Persistence;

// Tasks modulunun veritabani baglami. Yalnizca "tasks" semasini gorur.
public sealed class TasksDbContext(DbContextOptions<TasksDbContext> options) : DbContext(options)
{
    public const string Schema = "tasks";

    public DbSet<TransportTask> TransportTasks => Set<TransportTask>();

    public DbSet<Resource> Resources => Set<Resource>();

    public DbSet<ResourceLock> ResourceLocks => Set<ResourceLock>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    // ISIN KALBI: outbox satirlari SaveChanges'ten ONCE ekleniyor, boylece
    // durum degisikligiyle ayni transaction'a giriyorlar. Handler'lar bunu
    // ayrica cagirmak zorunda degil - unutulabilecek bir adim birakmiyoruz.
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
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

            // Ayni olayin ikinci bir SaveChanges'te tekrar yazilmamasi icin.
            aggregate.ClearDomainEvents();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TasksDbContext).Assembly);
    }
}
