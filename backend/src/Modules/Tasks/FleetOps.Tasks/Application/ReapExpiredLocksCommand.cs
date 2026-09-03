using FleetOps.SharedKernel;

namespace FleetOps.Tasks.Application;

// Suresi dolmus kilitleri serbest birakir. Kac tane birakildigini doner.
public sealed record ReapExpiredLocksCommand : ICommand<int>;
