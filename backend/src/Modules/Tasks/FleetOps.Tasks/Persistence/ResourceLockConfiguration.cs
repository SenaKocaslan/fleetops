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

        // Kaldirilirsa EF, anahtari dolu gelen yeni nesneye INSERT yerine UPDATE gonderir.
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.HasOne<Resource>()
            .WithMany()
            .HasForeignKey(l => l.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.AgvId).IsRequired();

        builder.Property(l => l.AcquiredAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(l => l.ExpiresAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(l => l.ReleasedAtUtc)
            .HasColumnType("timestamp with time zone");

        // "Bir kaynagin en fazla bir aktif kilidi olur" kuralini tutan tek sey bu
        // indeks; uygulama kodunda kontrol yok. Kaldirilirsa iki AGV ayni kaynagi
        // ayni anda kilitleyebilir.
        builder.HasIndex(l => l.ResourceId)
            .IsUnique()
            .HasFilter("released_at_utc IS NULL")
            .HasDatabaseName("ix_resource_lock_aktif_kaynak");

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
