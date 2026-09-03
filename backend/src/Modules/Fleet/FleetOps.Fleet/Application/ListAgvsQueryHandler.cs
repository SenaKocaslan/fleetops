using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Fleet.Application;

internal sealed class ListAgvsQueryHandler(FleetDbContext db)
    : IQueryHandler<ListAgvsQuery, IReadOnlyList<AgvSummary>>
{
    public async Task<Result<IReadOnlyList<AgvSummary>>> HandleAsync(
        ListAgvsQuery query,
        CancellationToken cancellationToken)
    {
        // Burada bilerek projeksiyon degil, entity yukluyorum: "gorev
        // alabilir mi" karari Agv.GorevAlabilir() icinde ve o kurali SQL'e
        // cevirmek icin tekrar yazmak, kuralin iki yerde durmasi demek.
        // Filo kucuk (onlarca arac), okuma maliyeti onemsiz.
        var agvler = await db.Agvs
            .AsNoTracking()
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);

        var sonuc = agvler
            .Select(a => new AgvSummary(
                a.Id,
                a.Code,
                a.Status.ToString(),
                a.BatteryLevel,
                a.GorevAlabilir()))
            .ToList();

        return Result.Success<IReadOnlyList<AgvSummary>>(sonuc);
    }
}
