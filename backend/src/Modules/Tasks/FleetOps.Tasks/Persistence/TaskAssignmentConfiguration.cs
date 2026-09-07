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

        builder.HasIndex(a => new { a.AgvId, a.CompletedAtUtc });

        builder.Ignore(a => a.Aktif);
    }
}
