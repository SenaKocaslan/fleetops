using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;

namespace FleetOps.Tasks.Application;

// MaterialCode aramasi sayfalamayla birlikte zorunlu hale geldi: 90 gorevlik
// bir havuzda, oncelige gore sirali listede yeni acilan dusuk oncelikli gorev
// ilk sayfada cikmiyor ve kullanici kendi actigi kaydi bulamiyor.
public sealed record ListTasksQuery(PageRequest Sayfa, string? MaterialCode = null)
    : IQuery<PagedResult<TaskSummary>>;
