using FleetOps.Tasks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetOps.Tasks.Persistence;

internal sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("resource");
        builder.HasKey(r => r.Id);

        // Kimlik domain'de uretilir; bkz. AgvConfiguration'daki aciklama.
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Code).HasMaxLength(32).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique();

        builder.Property(r => r.Kind)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Ignore(r => r.DomainEvents);

        // Tohum veri: kilit akisinin denenebilmesi icin kaynak olmali.
        builder.HasData(
            new
            {
                Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
                Code = "DOCK-1",
                Kind = ResourceKind.ChargingDock,
            },
            new
            {
                Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"),
                Code = "CORRIDOR-A",
                Kind = ResourceKind.Corridor,
            },
            new
            {
                Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"),
                Code = "LIFT-1",
                Kind = ResourceKind.Lift,
            });
    }
}
