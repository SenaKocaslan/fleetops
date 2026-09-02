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

        // Fleet modulundeki AGV'nin kimligi - FK YOK, sadece ID.
        builder.Property(a => a.AgvId).IsRequired();

        builder.Property(a => a.AssignedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(a => a.CompletedAtUtc)
            .HasColumnType("timestamp with time zone");

        // "Bu AGV'nin acik atamasi var mi?" sorgusu icin.
        builder.HasIndex(a => new { a.AgvId, a.CompletedAtUtc });

        builder.Ignore(a => a.Aktif);
    }
}
