using FleetOps.Stock.Domain;
using FleetOps.Stock.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetOps.Stock.Persistence;

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("location");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Code).HasMaxLength(32).IsRequired();
        builder.HasIndex(l => l.Code).IsUnique();
        builder.Property(l => l.Zone).HasMaxLength(32).IsRequired();

        builder.Ignore(l => l.DomainEvents);

        // Tohum veri: gorev olustururken gercek lokasyon secilebilsin.
        // Bu gelene kadar arayuz gecici Guid uretiyordu.
        builder.HasData(
            new { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000001"), Code = "KABUL-01", Zone = "Kabul" },
            new { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000002"), Code = "RAF-A1", Zone = "Depo" },
            new { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000003"), Code = "RAF-B2", Zone = "Depo" },
            new { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000004"), Code = "SEVK-01", Zone = "Sevkiyat" });
    }
}

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movement");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.MaterialCode).HasMaxLength(64).IsRequired();
        builder.Property(m => m.Quantity).IsRequired();

        // Lokasyonlar ayni modulde: FK var.
        builder.HasOne<Location>().WithMany()
            .HasForeignKey(m => m.FromLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Location>().WithMany()
            .HasForeignKey(m => m.ToLocationId).OnDelete(DeleteBehavior.Restrict);

        // Tasks modulundeki gorevin kimligi - FK YOK, sadece ID.
        builder.Property(m => m.SourceTaskId).IsRequired();

        builder.Property(m => m.MovedAtUtc)
            .HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(m => m.MovedAtUtc);

        builder.Ignore(m => m.DomainEvents);
    }
}

internal sealed class ProcessedIntegrationEventConfiguration
    : IEntityTypeConfiguration<ProcessedIntegrationEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedIntegrationEvent> builder)
    {
        builder.ToTable("processed_integration_event");

        // Birincil anahtar olayin kendi kimligi: ayni olayi iki kez
        // isaretlemek veritabani tarafindan reddedilir.
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.ProcessedAtUtc)
            .HasColumnType("timestamp with time zone").IsRequired();
    }
}
