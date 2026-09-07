using FleetOps.Tasks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetOps.Tasks.Persistence;

internal sealed class TransportTaskConfiguration : IEntityTypeConfiguration<TransportTask>
{
    public void Configure(EntityTypeBuilder<TransportTask> builder)
    {
        builder.ToTable("transport_task");
        builder.HasKey(t => t.Id);

        // Kaldirilirsa EF, anahtari dolu gelen yeni nesneye INSERT yerine UPDATE gonderir.
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.MaterialCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.Quantity).IsRequired();
        builder.Property(t => t.Priority).IsRequired();

        builder.Property(t => t.CreatedAtUtc)
            // Npgsql, Kind=Utc olmayan DateTime kabul etmez.
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(t => t.FromLocationId).IsRequired();
        builder.Property(t => t.ToLocationId).IsRequired();

        builder.HasIndex(t => new { t.Status, t.Priority });

        builder.Property(t => t.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasMany(t => t.Assignments)
            .WithOne()
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(TransportTask.Assignments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(t => t.DomainEvents);
    }
}
