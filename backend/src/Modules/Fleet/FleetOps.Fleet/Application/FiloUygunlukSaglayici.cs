using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Fleet.Application;

// Fleet'in disariya actigi tek okuma noktasi. Sorulari Fleet'in kendi domain
// metotlari cevapliyor; disaridaki modul durum adlarini yorumlamiyor.
internal sealed class FiloUygunlukSaglayici(FleetDbContext db) : IAgvUygunlukSaglayici
{
    public async Task<AgvUygunlugu> GetirAsync(Guid agvId, CancellationToken cancellationToken)
    {
        var agv = await db.Agvs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == agvId, cancellationToken);

        return agv is null
            ? AgvUygunlugu.Yok
            : new AgvUygunlugu(true, agv.Code, agv.GorevAlabilir(), agv.SahadaCalisabilir());
    }
}
