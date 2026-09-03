using FleetOps.SharedKernel;

namespace FleetOps.Tasks.Application;

public sealed record ReleaseLockCommand(Guid ResourceId, Guid AgvId) : ICommand;
