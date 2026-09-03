using FleetOps.SharedKernel;

namespace FleetOps.Tasks.Application;

public sealed record AssignTaskCommand(Guid TaskId, Guid AgvId) : ICommand;
