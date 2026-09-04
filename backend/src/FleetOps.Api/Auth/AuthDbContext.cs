using Microsoft.EntityFrameworkCore;

namespace FleetOps.Api.Auth;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public const string Schema = "auth";

    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        var kullanici = modelBuilder.Entity<AppUser>();
        kullanici.ToTable("app_user");
        kullanici.HasKey(k => k.Id);

        // Kaldirilirsa EF, anahtari dolu gelen yeni nesneye INSERT yerine
        // UPDATE gonderir.
        kullanici.Property(k => k.Id).ValueGeneratedNever();

        kullanici.Property(k => k.UserName).HasMaxLength(64).IsRequired();
        kullanici.HasIndex(k => k.UserName).IsUnique();

        kullanici.Property(k => k.PasswordHash).HasMaxLength(256).IsRequired();
        kullanici.Property(k => k.Role).HasMaxLength(32).IsRequired();

        // Tohum parolalar GELISTIRME icindir. Hash migration'a gomulu oldugu
        // icin sabit; tuz kullanici basina farkli. Uretimde bu iki satir
        // silinip kullanici kaydi ayri bir akistan gelmeli.
        kullanici.HasData(
            new
            {
                Id = Guid.Parse("dddddddd-0000-0000-0000-000000000001"),
                UserName = "operator",
                PasswordHash = "100000.u1dXkDE+WT5SEH2bOdEpZg==.fLwykOL+TgFQ7akkw/MlEPmcBiWkUBi/kxrCkh2WUg8=",
                Role = Roller.Operator,
            },
            new
            {
                Id = Guid.Parse("dddddddd-0000-0000-0000-000000000002"),
                UserName = "supervisor",
                PasswordHash = "100000.M5ugf+wFXLnNWojnB4Ok/Q==.dqXIJoJIK5JlCvxEKScj6oTIEkDUkArvkzqkICwGZAQ=",
                Role = Roller.Supervisor,
            });
    }
}
