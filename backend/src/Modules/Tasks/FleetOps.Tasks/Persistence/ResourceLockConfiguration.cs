using FleetOps.Tasks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetOps.Tasks.Persistence;

internal sealed class ResourceLockConfiguration : IEntityTypeConfiguration<ResourceLock>
{
    public void Configure(EntityTypeBuilder<ResourceLock> builder)
    {
        builder.ToTable("resource_lock");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).ValueGeneratedNever();

        // Kaynak ayni modulde: FK var. Modul disina giden alanlarda yok.
        builder.HasOne<Resource>()
            .WithMany()
            .HasForeignKey(l => l.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Fleet modulundeki AGV'nin kimligi - FK YOK, sadece ID.
        builder.Property(l => l.AgvId).IsRequired();

        builder.Property(l => l.AcquiredAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(l => l.ExpiresAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(l => l.ReleasedAtUtc)
            .HasColumnType("timestamp with time zone");

        // ISIN KALBI: "bir kaynagin en fazla bir aktif kilidi olur."
        // Bu kural tek bir aggregate'in icinde degil, satirlar arasinda.
        // Optimistic concurrency onu koruyamaz: henuz var olmayan satir icin
        // karsilastirilacak bir surum yok. Bu yuzden kurali veritabani
        // uyguluyor - kismi tekil indeks, yalnizca birakilmamis kilitleri
        // kapsiyor, birakilanlar gecmis olarak durmaya devam ediyor.
        builder.HasIndex(l => l.ResourceId)
            .IsUnique()
            .HasFilter("released_at_utc IS NULL")
            .HasDatabaseName("ix_resource_lock_aktif_kaynak");

        // Reaper'in "suresi dolmus aktif kilitler" sorgusu icin.
        builder.HasIndex(l => new { l.ReleasedAtUtc, l.ExpiresAtUtc });

        builder.Property(l => l.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Ignore(l => l.Aktif);
        builder.Ignore(l => l.DomainEvents);
    }
}
