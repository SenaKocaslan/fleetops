using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Stock.Domain;
using FleetOps.Stock.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Stock.Application;

public sealed record StockBalanceSummary(
    Guid LocationId,
    string LocationCode,
    string MaterialCode,
    int Quantity);

public sealed record ListStockBalancesQuery(PageRequest Sayfa, string? MaterialCode = null)
    : IQuery<PagedResult<StockBalanceSummary>>;

// Bakiye ayri bir tabloda TUTULMUYOR, hareketlerden hesaplaniyor. Ayri tablo
// ayni bilgiyi iki yerde tutmak ve ikisinin ayrismasini beklemek demekti.
// Hareket sayisi milyonlara cikarsa her istekte GROUP BY pahalilasir; o zaman
// hareket eklenirken guncellenen bir ozet tabloya gecilir.
// Sorgu icinde konumsal kayit (record) DEGIL, nesne baslaticili bir sinif:
// EF, kurucuyla olusturulan nesnenin alanina gore siralamayi SQL'e
// ceviremiyor (olculdu: "could not be translated" ile 500). Kayda donusum
// en sonda, sayfalamadan sonra yapiliyor.
internal sealed class BakiyeSatiri
{
    public Guid LocationId { get; init; }

    public string LocationCode { get; init; } = string.Empty;

    public string MaterialCode { get; init; } = string.Empty;

    public int Quantity { get; init; }
}

internal static class BakiyeSorgusu
{
    public static IQueryable<BakiyeSatiri> DepoBakiyeleri(StockDbContext db)
    {
        var girisler = db.StockMovements.Select(m => new
        {
            Lokasyon = m.ToLocationId,
            m.MaterialCode,
            Miktar = m.Quantity,
        });

        var cikislar = db.StockMovements.Select(m => new
        {
            Lokasyon = m.FromLocationId,
            m.MaterialCode,
            Miktar = -m.Quantity,
        });

        return girisler.Concat(cikislar)
            .GroupBy(h => new { h.Lokasyon, h.MaterialCode })
            .Select(g => new { g.Key.Lokasyon, g.Key.MaterialCode, Miktar = g.Sum(h => h.Miktar) })
            .Where(b => b.Miktar != 0)
            .Join(
                db.Locations.Where(l => l.Zone == Bolgeler.Depo),
                b => b.Lokasyon,
                l => l.Id,
                (b, l) => new BakiyeSatiri
                {
                    LocationId = l.Id,
                    LocationCode = l.Code,
                    MaterialCode = b.MaterialCode,
                    Quantity = b.Miktar,
                });
    }
}

internal sealed class ListStockBalancesQueryHandler(StockDbContext db)
    : IQueryHandler<ListStockBalancesQuery, PagedResult<StockBalanceSummary>>
{
    public async Task<Result<PagedResult<StockBalanceSummary>>> HandleAsync(
        ListStockBalancesQuery query,
        CancellationToken cancellationToken)
    {
        var sorgu = BakiyeSorgusu.DepoBakiyeleri(db);

        if (!string.IsNullOrWhiteSpace(query.MaterialCode))
        {
            var ara = query.MaterialCode.Trim();
            sorgu = sorgu.Where(b => EF.Functions.ILike(b.MaterialCode, $"%{ara}%"));
        }

        var toplam = await sorgu.CountAsync(cancellationToken);

        // Lokasyon + malzeme zaten tekil (gruplama anahtari); sayfalama icin
        // tam siralama olusturuyor, ayrica esitlik bozucu gerekmiyor.
        var kayitlar = await sorgu
            .OrderBy(b => b.LocationCode)
            .ThenBy(b => b.MaterialCode)
            .Skip(query.Sayfa.Atlanacak)
            .Take(query.Sayfa.PageSize)
            .Select(b => new StockBalanceSummary(b.LocationId, b.LocationCode, b.MaterialCode, b.Quantity))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<StockBalanceSummary>(
            kayitlar, query.Sayfa.Page, query.Sayfa.PageSize, toplam));
    }
}

// Rafta eksi bakiye: kaydi tutulmamis bir hareket var demek. Malzeme o rafa
// sistem disinda gelmis ya da tasima gorevi olmayan bir yerden malzeme
// almis. Iki durumda da sayim ile kayit ayrismis; birinin bakmasi gerekiyor.
internal sealed class StokAlarmKaynagi(StockDbContext db) : IAlarmSource
{
    public async Task<IReadOnlyList<AlarmSummary>> AlarmlariGetirAsync(
        CancellationToken cancellationToken)
    {
        var simdi = DateTime.UtcNow;

        var eksiler = await BakiyeSorgusu.DepoBakiyeleri(db)
            .Where(b => b.Quantity < 0)
            .OrderBy(b => b.LocationCode)
            .ThenBy(b => b.MaterialCode)
            .ToListAsync(cancellationToken);

        return eksiler
            .Select(b => new AlarmSummary(
                "Stock.EksiBakiye",
                AlarmSeverity.Uyari,
                $"{b.LocationCode} / {b.MaterialCode}",
                $"Kayitlara gore bakiye {b.Quantity}. Rafa sistem disinda malzeme gelmis "
                    + "ya da kaydi olmayan bir cikis yapilmis olabilir.",
                simdi))
            .ToList();
    }
}
