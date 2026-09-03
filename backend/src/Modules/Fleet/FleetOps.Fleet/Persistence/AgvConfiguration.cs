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

        // Kimlik domain'de uretilir (factory metodu Guid'i kendisi verir).
        // Bunu soylemezsek EF, Guid anahtari "veritabani uretir" sayar ve
        // anahtari dolu gelen yeni nesneyi "zaten var olan satir" zannedip
        // INSERT yerine UPDATE gonderir.
        builder.Property(a => a.Id).ValueGeneratedNever();


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


        // Tohum veri: atama akisinin calisabilmesi icin filoda arac olmali.
        // Acilista calisan tohumlama koduna gore avantaji, cok instance'li
        // dagitimda yaris kosulu olusturmamasi. AGV kayit akisi (POST
        // /api/agvs) yazildiginda bu tohum kaldirilacak.
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

        // Domain event'ler bellekte tutulur, veritabanina yazilmaz.
        builder.Ignore(a => a.DomainEvents);
    }
}
