using FleetOps.Fleet.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetOps.Fleet.Persistence;

internal sealed class AgvConfiguration : IEntityTypeConfiguration<Agv>
{
    public void Configure(EntityTypeBuilder<Agv> builder)
    {
        builder.ToTable("agv");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(a => a.Code).IsUnique();

        // Enum string olarak saklanir: veritabanina bakildiginda "Available"
        // gorunur, "1" degil. Hata ayiklamada fark yaratir.
        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.BatteryLevel).IsRequired();

        // Stock modulundeki lokasyonun kimligi - FK YOK, sadece ID.
        builder.Property(a => a.CurrentLocationId);

        // Optimistic concurrency: PostgreSQL'in xmin sistem sutunu.
        // Ayri bir row_version sutunu tutmaya gerek yok, veritabani zaten
        // her satirin son degistiren transaction'ini biliyor.
        builder.Property(a => a.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Domain event'ler bellekte tutulur, veritabanina yazilmaz.
        builder.Ignore(a => a.DomainEvents);
    }
}
