using FleetOps.SharedKernel;

namespace FleetOps.Tasks.Application;

public sealed record ListTasksQuery : IQuery<IReadOnlyList<TaskSummary>>;
