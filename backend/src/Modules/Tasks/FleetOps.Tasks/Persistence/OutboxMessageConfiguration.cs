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

        // jsonb: metin degil, sorgulanabilir JSON. Hatali payload'i
        // veritabaninda ayiklayabilmek icin degerli.
        builder.Property(m => m.Payload).HasColumnType("jsonb").IsRequired();

        builder.Property(m => m.OccurredAtUtc)
            .HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(m => m.ProcessedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(m => m.Error).HasMaxLength(2000);

        // Daginin sorgusu: islenmemisleri olus sirasina gore al.
        builder.HasIndex(m => new { m.ProcessedAtUtc, m.OccurredAtUtc });
    }
}
