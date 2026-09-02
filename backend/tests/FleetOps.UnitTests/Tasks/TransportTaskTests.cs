using FleetOps.Tasks.Domain;

namespace FleetOps.UnitTests.Tasks;

public class TransportTaskTests
{
    private static readonly Guid Kaynak = Guid.NewGuid();
    private static readonly Guid Hedef = Guid.NewGuid();
    private static readonly DateTime Simdi = new(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc);

    private static TransportTask Yeni() =>
        TransportTask.Create(Guid.NewGuid(), Kaynak, Hedef, "MLZ-100", 5, 1, Simdi).Value;

    // Gorevi istenen duruma getirir; gecis matrisi testinde kullanilir.
    private static TransportTask DurumdakiGorev(TransportTaskStatus durum)
    {
        var gorev = Yeni();
        switch (durum)
        {
            case TransportTaskStatus.Pending:
                break;
            case TransportTaskStatus.Assigned:
                gorev.Assign(Guid.NewGuid(), Simdi);
                break;
            case TransportTaskStatus.InProgress:
                gorev.Assign(Guid.NewGuid(), Simdi);
                gorev.Start();
                break;
            case TransportTaskStatus.Completed:
                gorev.Assign(Guid.NewGuid(), Simdi);
                gorev.Start();
                gorev.Complete(Simdi);
                break;
            case TransportTaskStatus.Failed:
                gorev.Assign(Guid.NewGuid(), Simdi);
                gorev.Start();
                gorev.Fail(Simdi);
                break;
            case TransportTaskStatus.Cancelled:
                gorev.Cancel();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(durum));
        }

        Assert.Equal(durum, gorev.Status);
        return gorev;
    }

    // ---------- olusturma dogrulamalari ----------

    [Fact]
    public void Ayni_lokasyona_tasima_gorevi_olusturulamaz()
    {
        var sonuc = TransportTask.Create(Guid.NewGuid(), Kaynak, Kaynak, "MLZ-100", 5, 1, Simdi);

        Assert.Equal(TaskErrors.AyniLokasyon, sonuc.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Miktar_pozitif_olmali(int miktar)
    {
        var sonuc = TransportTask.Create(Guid.NewGuid(), Kaynak, Hedef, "MLZ-100", miktar, 1, Simdi);

        Assert.Equal(TaskErrors.MiktarPozitifOlmali, sonuc.Error);
    }

    [Fact]
    public void Malzeme_kodu_bos_olamaz()
    {
        var sonuc = TransportTask.Create(Guid.NewGuid(), Kaynak, Hedef, "  ", 5, 1, Simdi);

        Assert.Equal(TaskErrors.MalzemeKoduBos, sonuc.Error);
    }

    [Fact]
    public void Yeni_gorev_havuzda_ve_atamasiz_baslar()
    {
        var gorev = Yeni();

        Assert.Equal(TransportTaskStatus.Pending, gorev.Status);
        Assert.Null(gorev.AktifAtama);
        Assert.Empty(gorev.Assignments);
    }

    // ---------- gecis matrisi: TAM kapsam ----------

    public static TheoryData<TransportTaskStatus, TransportTaskStatus, bool> GecisMatrisi()
    {
        var izinli = new HashSet<(TransportTaskStatus, TransportTaskStatus)>
        {
            (TransportTaskStatus.Pending, TransportTaskStatus.Assigned),
            (TransportTaskStatus.Pending, TransportTaskStatus.Cancelled),
            (TransportTaskStatus.Assigned, TransportTaskStatus.InProgress),
            (TransportTaskStatus.Assigned, TransportTaskStatus.Pending),
            (TransportTaskStatus.InProgress, TransportTaskStatus.Completed),
            (TransportTaskStatus.InProgress, TransportTaskStatus.Failed),
        };

        var veri = new TheoryData<TransportTaskStatus, TransportTaskStatus, bool>();
        foreach (var kaynak in Enum.GetValues<TransportTaskStatus>())
        {
            foreach (var hedef in Enum.GetValues<TransportTaskStatus>())
            {
                veri.Add(kaynak, hedef, izinli.Contains((kaynak, hedef)));
            }
        }

        return veri;
    }

    [Theory]
    [MemberData(nameof(GecisMatrisi))]
    public void Yalnizca_izinli_gecisler_kabul_edilir(
        TransportTaskStatus kaynak,
        TransportTaskStatus hedef,
        bool izinliMi)
    {
        var gorev = DurumdakiGorev(kaynak);

        var sonuc = hedef switch
        {
            TransportTaskStatus.Assigned => gorev.Assign(Guid.NewGuid(), Simdi),
            TransportTaskStatus.Pending => gorev.Release(Simdi),
            TransportTaskStatus.InProgress => gorev.Start(),
            TransportTaskStatus.Completed => gorev.Complete(Simdi),
            TransportTaskStatus.Failed => gorev.Fail(Simdi),
            TransportTaskStatus.Cancelled => gorev.Cancel(),
            _ => throw new ArgumentOutOfRangeException(nameof(hedef)),
        };

        Assert.Equal(izinliMi, sonuc.IsSuccess);
        Assert.Equal(izinliMi ? hedef : kaynak, gorev.Status);
    }

    // ---------- atama davranisi ----------

    [Fact]
    public void Atama_yapilinca_aktif_atama_olusur()
    {
        var gorev = Yeni();
        var agvId = Guid.NewGuid();

        gorev.Assign(agvId, Simdi);

        Assert.NotNull(gorev.AktifAtama);
        Assert.Equal(agvId, gorev.AktifAtama!.AgvId);
        Assert.Equal(Simdi, gorev.AktifAtama.AssignedAtUtc);
    }

    [Fact]
    public void Bos_agv_kimligiyle_atanamaz()
    {
        Assert.Equal(TaskErrors.AgvBos, Yeni().Assign(Guid.Empty, Simdi).Error);
    }

    [Fact]
    public void Serbest_birakilinca_atama_kapanir_ve_gorev_havuza_doner()
    {
        var gorev = DurumdakiGorev(TransportTaskStatus.Assigned);
        var birakma = Simdi.AddMinutes(5);

        gorev.Release(birakma);

        Assert.Equal(TransportTaskStatus.Pending, gorev.Status);
        Assert.Null(gorev.AktifAtama);
        Assert.Equal(birakma, gorev.Assignments.Single().CompletedAtUtc);
    }

    [Fact]
    public void Reddedilen_gorev_baska_agvye_atanabilir_ve_gecmis_korunur()
    {
        var gorev = Yeni();
        var ilkAgv = Guid.NewGuid();
        var ikinciAgv = Guid.NewGuid();

        gorev.Assign(ilkAgv, Simdi);
        gorev.Release(Simdi.AddMinutes(1));
        gorev.Assign(ikinciAgv, Simdi.AddMinutes(2));

        Assert.Equal(2, gorev.Assignments.Count);
        Assert.Equal(ikinciAgv, gorev.AktifAtama!.AgvId);
        Assert.Single(gorev.Assignments, a => a.AgvId == ilkAgv && !a.Aktif);
    }

    [Fact]
    public void Tamamlanan_gorevin_atamasi_kapanir()
    {
        var gorev = DurumdakiGorev(TransportTaskStatus.InProgress);
        var bitis = Simdi.AddMinutes(10);

        gorev.Complete(bitis);

        Assert.Equal(TransportTaskStatus.Completed, gorev.Status);
        Assert.Null(gorev.AktifAtama);
        Assert.Equal(bitis, gorev.Assignments.Single().CompletedAtUtc);
    }
}
