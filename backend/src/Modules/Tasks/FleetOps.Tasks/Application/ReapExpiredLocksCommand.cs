using FleetOps.SharedKernel;

namespace FleetOps.Tasks.Application;

public sealed record ReapExpiredLocksCommand : ICommand<int>;
