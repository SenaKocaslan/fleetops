using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Infrastructure;
using FleetOps.Fleet.Persistence;
using FleetOps.IntegrationTests.Altyapi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class SarjYonlendirmeTests(FleetOpsApiFactory fabrika)
{
    private static readonly Guid Agv01 = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Bataryasi_dusen_musait_arac_sarja_alinir()
    {
        var esik = Ayarlar().SarjaGonderEsigi;
        await AgvAyarlaAsync(AgvStatus.Available, esik - 1);

        var (sarjaGiden, _) = await TurCalistirAsync();

        Assert.True(sarjaGiden >= 1);
        Assert.Equal(AgvStatus.Charging, (await AgvOkuAsync()).Status);
    }

    [Fact]
    public async Task Esigin_ustundeki_arac_sarja_alinmaz()
    {
        // Kontrol testi: yonlendirici her araci sarja alsaydi yukaridaki
        // test de yesil yanardi.
        var esik = Ayarlar().SarjaGonderEsigi;
        await AgvAyarlaAsync(AgvStatus.Available, esik);

        await TurCalistirAsync();

        Assert.Equal(AgvStatus.Available, (await AgvOkuAsync()).Status);
    }

    [Fact]
    public async Task Mesgul_arac_bataryasi_dusuk_olsa_da_sarja_alinmaz()
    {
        // Yurutulen gorev yarida kalirdi ve gorev havuza donmedigi icin
        // kimse fark etmezdi. Arac gorevi bitirince siradaki tur onu alir.
        await AgvAyarlaAsync(AgvStatus.Busy, 1);

        await TurCalistirAsync();

        Assert.Equal(AgvStatus.Busy, (await AgvOkuAsync()).Status);
    }

    [Fact]
    public async Task Sarji_dolan_arac_servise_doner()
    {
        var esik = Ayarlar().SarjdanDonEsigi;
        await AgvAyarlaAsync(AgvStatus.Charging, esik);

        var (_, serviseDonen) = await TurCalistirAsync();

        Assert.True(serviseDonen >= 1);
        Assert.Equal(AgvStatus.Available, (await AgvOkuAsync()).Status);
    }

    [Fact]
    public async Task Sarji_yetersiz_arac_serviste_donmez()
    {
        var esik = Ayarlar().SarjdanDonEsigi;
        await AgvAyarlaAsync(AgvStatus.Charging, esik - 1);

        await TurCalistirAsync();

        Assert.Equal(AgvStatus.Charging, (await AgvOkuAsync()).Status);
    }

    [Fact]
    public async Task Iki_esik_arasinda_salinim_olmaz()
    {
        // HISTEREZIS TESTI: tek esik olsaydi sarja gonderme esigindeki bir
        // arac her turda sarja gir/cik yapardi. Iki esik arasindaki araca
        // ust uste turlar uygulaniyor; durum sabit kalmali.
        var ayarlar = Ayarlar();
        var orta = (ayarlar.SarjaGonderEsigi + ayarlar.SarjdanDonEsigi) / 2;
        await AgvAyarlaAsync(AgvStatus.Charging, orta);

        for (var i = 0; i < 3; i++)
        {
            await TurCalistirAsync();
            Assert.Equal(AgvStatus.Charging, (await AgvOkuAsync()).Status);
        }
    }

    [Fact]
    public void Yonlendirici_barindirilan_servis_olarak_kayitli()
    {
        Assert.Single(fabrika.Services.GetServices<IHostedService>().OfType<SarjYonlendirici>());
    }

    private SarjOptions Ayarlar() =>
        fabrika.Services.GetRequiredService<IOptions<SarjOptions>>().Value;

    private Task<(int SarjaGiden, int ServiseDonen)> TurCalistirAsync() =>
        fabrika.Services.GetServices<IHostedService>()
            .OfType<SarjYonlendirici>().Single()
            .BirTurCalistirAsync(CancellationToken.None);

    private async Task AgvAyarlaAsync(AgvStatus durum, int batarya)
    {
        // Diger araclar turu etkilemesin: yalnizca AGV-01 sahnede.
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();

        foreach (var diger in await db.Agvs.Where(a => a.Id != Agv01).ToListAsync())
        {
            diger.ServisDisiBirak();
        }

        var agv = await db.Agvs.SingleAsync(a => a.Id == Agv01);

        // Once durum, SONRA batarya. Mesgullestir() dusuk bataryali araci
        // zaten reddeder; gercek senaryo da bu sirada olusuyor: arac gorevi
        // alirken doluydu, gorev sirasinda bataryasi dustu.
        agv.ServiseAl();
        agv.BataryaBildir(100);

        switch (durum)
        {
            case AgvStatus.Charging: agv.SarjaAl(); break;
            case AgvStatus.Busy: agv.Mesgullestir(); break;
            case AgvStatus.Available: break;
            default: agv.ServisDisiBirak(); break;
        }

        agv.BataryaBildir(batarya);

        await db.SaveChangesAsync();
    }

    private async Task<Agv> AgvOkuAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        return await db.Agvs.AsNoTracking().SingleAsync(a => a.Id == Agv01);
    }
}
