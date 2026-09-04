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

        // Kaldirilirsa EF, anahtari dolu gelen yeni nesneye INSERT yerine UPDATE gonderir.
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(a => a.Code).IsUnique();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.BatteryLevel).IsRequired();

        builder.Property(a => a.CurrentLocationId);

        builder.Property(a => a.LastSeenAtUtc);

        builder.Property(a => a.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasData(
            new
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Code = "AGV-01",
                Status = AgvStatus.Available,
                BatteryLevel = 95,
            },
            new
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Code = "AGV-02",
                Status = AgvStatus.Available,
                BatteryLevel = 60,
            },
            new
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Code = "AGV-03",
                Status = AgvStatus.Charging,
                BatteryLevel = 12,
            });

        builder.Ignore(a => a.DomainEvents);
    }
}
