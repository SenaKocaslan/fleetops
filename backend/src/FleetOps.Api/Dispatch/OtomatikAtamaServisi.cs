using FleetOps.Fleet.Application;
using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Application;
using FleetOps.Tasks.Domain;
using Microsoft.Extensions.Logging;

namespace FleetOps.Api.Dispatch;

public sealed record AtamaSonucu(
    Guid TaskId,
    string MaterialCode,
    int Priority,
    Guid AgvId,
    string AgvCode);

public sealed record OtomatikAtamaOzeti(
    IReadOnlyList<AtamaSonucu> Atananlar,
    int BekleyenGorev,
    int MusaitAgv,
    string Strateji);

// NEDEN COMPOSITION ROOT'TA: "hangi goreve hangi arac" sorusu iki modulun
// verisini birden gerektiriyor. Tasks, Fleet'in AGV'lerini goremez; Fleet de
// gorev havuzunu gormez. Ikisini birden goren tek yer burasi -- alarmlarin
// birlestirildigi yerle ayni gerekce. Moduller yine birbirini cagirmiyor;
// bu servis her ikisinin kendi uygulama arayuzunu kullaniyor.
public sealed class OtomatikAtamaServisi(
    IQueryHandler<ListAgvsQuery, IReadOnlyList<AgvSummary>> agvSorgusu,
    IQueryHandler<ListTasksQuery, PagedResult<TaskSummary>> gorevSorgusu,
    ICommandHandler<AssignTaskCommand> atamaKomutu,
    IAtamaStratejisi strateji,
    ILogger<OtomatikAtamaServisi> logger)
{
    public async Task<Result<OtomatikAtamaOzeti>> CalistirAsync(
        CancellationToken cancellationToken)
    {
        var agvler = await agvSorgusu.HandleAsync(new ListAgvsQuery(), cancellationToken);
        if (agvler.IsFailure)
        {
            return Result.Failure<OtomatikAtamaOzeti>(agvler.Error);
        }

        var adaylar = agvler.Value
            .Where(a => a.GorevAlabilir)
            .Select(a => new AtamaAdayi(a.Id, a.Code, a.BatteryLevel))
            .ToList();

        // Musait arac sayisindan fazla gorev cekmenin anlami yok.
        var gorevler = await gorevSorgusu.HandleAsync(
            new ListTasksQuery(
                new PageRequest(1, Math.Max(adaylar.Count, 1)),
                Status: TransportTaskStatus.Pending),
            cancellationToken);

        if (gorevler.IsFailure)
        {
            return Result.Failure<OtomatikAtamaOzeti>(gorevler.Error);
        }

        var atananlar = new List<AtamaSonucu>();

        foreach (var gorev in gorevler.Value.Items)
        {
            if (adaylar.Count == 0)
            {
                break;
            }

            var secilen = strateji.Sec(adaylar);
            if (secilen is null)
            {
                break;
            }

            var sonuc = await atamaKomutu.HandleAsync(
                new AssignTaskCommand(gorev.Id, secilen.AgvId), cancellationToken);

            if (sonuc.IsSuccess)
            {
                // Arac artik mesgul. Kritik nokta su: Fleet bunu ancak outbox
                // olayi teslim edildikten SONRA ogrenir, yani AGV sorgusunu
                // bastan cekmek hala "musait" derdi. Ayni araca iki gorev
                // vermeyi engelleyen sey bu yerel liste -- ve son bekci olarak
                // task_assignment uzerindeki kismi tekil indeks.
                adaylar.Remove(secilen);

                atananlar.Add(new AtamaSonucu(
                    gorev.Id, gorev.MaterialCode, gorev.Priority, secilen.AgvId, secilen.Code));

                continue;
            }

            // Arac gercekten mesgulse aday listesinden cikar. Baska bir sebeple
            // (gorev bu arada atanmis, gecersiz gecis) basarisiz olduysa arac
            // hala bos: onu harcamak yerine sonraki goreve birakiyoruz.
            if (sonuc.Error == TaskErrors.AgvMesgul)
            {
                adaylar.Remove(secilen);
            }

            logger.LogInformation(
                "Otomatik atama gorevi atlamak zorunda kaldi: {GorevId} -> {AgvKodu} ({Hata})",
                gorev.Id, secilen.Code, sonuc.Error.Code);
        }

        return Result.Success(new OtomatikAtamaOzeti(
            atananlar,
            gorevler.Value.TotalCount,
            agvler.Value.Count(a => a.GorevAlabilir),
            strateji.Ad));
    }
}
