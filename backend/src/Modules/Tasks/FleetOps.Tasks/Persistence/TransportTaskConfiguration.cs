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

        // Kimlik domain'de uretilir (factory metodu Guid'i kendisi verir).
        // Bunu soylemezsek EF, Guid anahtari "veritabani uretir" sayar ve
        // anahtari dolu gelen yeni nesneyi "zaten var olan satir" zannedip
        // INSERT yerine UPDATE gonderir.
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

        // Npgsql, Kind=Utc olmayan DateTime kabul etmez; tum zamanlar UTC.
        builder.Property(t => t.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Stock modulundeki lokasyon kimlikleri - FK YOK.
        builder.Property(t => t.FromLocationId).IsRequired();
        builder.Property(t => t.ToLocationId).IsRequired();

        // Gorev havuzu sorgusu: bekleyenleri onceligine gore sirala.
        builder.HasIndex(t => new { t.Status, t.Priority });

        builder.Property(t => t.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Atamalar aggregate'in parcasi: yalnizca gorev uzerinden yuklenir.
        builder.HasMany(t => t.Assignments)
            .WithOne()
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // EF listeyi dogrudan alandan doldurur; Assignments property'si
        // salt okunur kalir, kapsulleme bozulmaz.
        builder.Metadata
            .FindNavigation(nameof(TransportTask.Assignments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(t => t.DomainEvents);
    }
}
