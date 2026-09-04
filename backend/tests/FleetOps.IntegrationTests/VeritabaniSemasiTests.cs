using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Persistence;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class VeritabaniSemasiTests(FleetOpsApiFactory fabrika)
{
    [Fact]
    public async Task Her_modul_kendi_semasina_migration_uygular()
    {
        using var kapsam = fabrika.KapsamAc();
        var fleet = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();

        var semalar = await fleet.Database
            .SqlQuery<string>($"SELECT nspname AS \"Value\" FROM pg_namespace WHERE nspname IN ('fleet','tasks')")
            .ToListAsync();

        Assert.Contains("fleet", semalar);
        Assert.Contains("tasks", semalar);
    }

    [Fact]
    public async Task Modul_disina_foreign_key_yoktur()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();

        var modulDisiFkler = await db.Database.SqlQuery<string>($"""
            SELECT c.conname AS "Value"
            FROM pg_constraint c
            JOIN pg_class     kaynak ON kaynak.oid = c.conrelid
            JOIN pg_namespace ks     ON ks.oid = kaynak.relnamespace
            JOIN pg_class     hedef  ON hedef.oid = c.confrelid
            JOIN pg_namespace hs     ON hs.oid = hedef.relnamespace
            WHERE c.contype = 'f'
              AND ks.nspname IN ('fleet','tasks')
              AND hs.nspname <> ks.nspname
            """).ToListAsync();

        Assert.Empty(modulDisiFkler);
    }

    [Fact]
    public async Task Agv_kaydedilip_geri_okunur_ve_durum_metin_olarak_saklanir()
    {
        var id = Guid.NewGuid();
        var kod = $"AGV-{Guid.NewGuid().ToString()[..8]}";

        using (var kapsam = fabrika.KapsamAc())
        {
            var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
            db.Agvs.Add(Agv.Register(id, kod, 85).Value);
            await db.SaveChangesAsync();
        }

        using (var kapsam = fabrika.KapsamAc())
        {
            var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
            var agv = await db.Agvs.SingleAsync(a => a.Id == id);

            Assert.Equal(kod, agv.Code);
            Assert.Equal(85, agv.BatteryLevel);
            Assert.Equal(AgvStatus.Available, agv.Status);
            Assert.NotEqual(0u, agv.Version); // xmin okundu

            var metin = await db.Database
                .SqlQuery<string>($"SELECT status AS \"Value\" FROM fleet.agv WHERE id = {id}")
                .SingleAsync();
            Assert.Equal("Available", metin);
        }
    }

    [Fact]
    public async Task Gorev_ve_atamalari_bir_butun_olarak_yuklenir()
    {
        var id = Guid.NewGuid();
        var agvId = Guid.NewGuid();

        using (var kapsam = fabrika.KapsamAc())
        {
            var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
            var gorev = TransportTask.Create(
                id, Guid.NewGuid(), Guid.NewGuid(), "MLZ-100", 5, 1, DateTime.UtcNow).Value;
            gorev.Assign(agvId, DateTime.UtcNow);

            db.TransportTasks.Add(gorev);
            await db.SaveChangesAsync();
        }

        using (var kapsam = fabrika.KapsamAc())
        {
            var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
            var gorev = await db.TransportTasks
                .Include(t => t.Assignments)
                .SingleAsync(t => t.Id == id);

            Assert.Equal(TransportTaskStatus.Assigned, gorev.Status);
            Assert.NotNull(gorev.AktifAtama);
            Assert.Equal(agvId, gorev.AktifAtama!.AgvId);
        }
    }

    [Fact]
    public async Task Utc_olmayan_tarih_npgsql_tarafindan_reddedilir()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();

        var yerelSaat = new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Local);
        var gorev = TransportTask.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "MLZ-100", 1, 1, yerelSaat).Value;
        db.TransportTasks.Add(gorev);

        var hata = await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
        var kok = hata.InnerException ?? hata;

        // Herhangi bir hata degil, TAM OLARAK Kind hatasi bekleniyor:
        // aksi halde test yanlis sebeple yesil yanar.
        Assert.IsType<ArgumentException>(kok);
        Assert.Contains("Kind=Local", kok.Message, StringComparison.Ordinal);
    }
}
