using FleetOps.Tasks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetOps.Tasks.Persistence;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_message");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Type).HasMaxLength(128).IsRequired();

        builder.Property(m => m.Payload).HasColumnType("jsonb").IsRequired();

        builder.Property(m => m.OccurredAtUtc)
            .HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(m => m.ProcessedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(m => m.Error).HasMaxLength(2000);

        builder.Property(m => m.DeadLetteredAtUtc).HasColumnType("timestamp with time zone");

        // Daginin sorgusu iki alani da filtreliyor; indeks ikisini de
        // kapsamazsa kuyruk buyudukce tarama tum tabloya doner.
        builder.HasIndex(m => new { m.ProcessedAtUtc, m.DeadLetteredAtUtc, m.OccurredAtUtc });
    }
}
