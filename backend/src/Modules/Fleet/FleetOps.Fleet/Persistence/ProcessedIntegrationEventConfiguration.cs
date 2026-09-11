using FleetOps.Fleet.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetOps.Fleet.Persistence;

internal sealed class ProcessedIntegrationEventConfiguration
    : IEntityTypeConfiguration<ProcessedIntegrationEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedIntegrationEvent> builder)
    {
        builder.ToTable("processed_integration_event");

        // Birincil anahtar olayin kendi kimligi: ayni olayi iki kez islemeyi
        // veritabani reddeder. Idempotentligi saglayan asil sey bu.
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.ProcessedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
