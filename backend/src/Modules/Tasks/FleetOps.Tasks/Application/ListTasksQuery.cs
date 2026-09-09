using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Domain;

namespace FleetOps.Tasks.Application;

// MaterialCode aramasi sayfalamayla birlikte zorunlu hale geldi: 90 gorevlik
// bir havuzda, oncelige gore sirali listede yeni acilan dusuk oncelikli gorev
// ilk sayfada cikmiyor ve kullanici kendi actigi kaydi bulamiyor.
// Status filtresi HTTP uc noktasinda ACIK DEGIL; bugun yalnizca otomatik
// atama kullaniyor ("havuzdaki bekleyen gorevler"). Kullanicinin isteyecegi
// bir filtre oldugunda uc noktaya da baglanabilir.
public sealed record ListTasksQuery(
    PageRequest Sayfa,
    string? MaterialCode = null,
    TransportTaskStatus? Status = null)
    : IQuery<PagedResult<TaskSummary>>;
