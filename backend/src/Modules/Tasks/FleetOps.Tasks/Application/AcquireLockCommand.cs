using FleetOps.SharedKernel;

namespace FleetOps.Tasks.Application;

public sealed record AcquireLockCommand(Guid ResourceId, Guid AgvId) : ICommand<Guid>;
