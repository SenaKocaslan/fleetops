using FleetOps.SharedKernel;

namespace FleetOps.Tasks.Application;

public sealed record ListResourcesQuery : IQuery<IReadOnlyList<ResourceSummary>>;
