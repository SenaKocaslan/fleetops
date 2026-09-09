using FleetOps.Tasks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetOps.Tasks.Persistence;

internal sealed class TaskAssignmentConfiguration : IEntityTypeConfiguration<TaskAssignment>
{
    public void Configure(EntityTypeBuilder<TaskAssignment> builder)
    {
        builder.ToTable("task_assignment");
        builder.HasKey(a => a.Id);

        // Kaldirilirsa EF, anahtari dolu gelen yeni nesneye INSERT yerine UPDATE gonderir.
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.AgvId).IsRequired();

        builder.Property(a => a.AssignedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(a => a.CompletedAtUtc)
            .HasColumnType("timestamp with time zone");

        // ISIN KALBI: "bir AGV ayni anda tek bir gorev yurutur." Kural tek bir
        // aggregate'in icinde degil, satirlar arasinda: gorev kendi atamalarini
        // gorur ama baska bir gorevin atamalarini gormez. Optimistic concurrency
        // de koruyamaz, cunku henuz var olmayan satir icin karsilastirilacak bir
        // surum yok. Bu yuzden kurali veritabani uyguluyor -- kismi tekil indeks,
        // yalnizca kapanmamis atamalari kapsiyor; kapananlar gecmis olarak kaliyor.
        builder.HasIndex(a => a.AgvId)
            .IsUnique()
            .HasFilter("completed_at_utc IS NULL");

        builder.Ignore(a => a.Aktif);
    }
}
