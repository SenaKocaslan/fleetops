using FleetOps.SharedKernel;

namespace FleetOps.Fleet.Application;

public sealed record ListAgvsQuery : IQuery<IReadOnlyList<AgvSummary>>;
