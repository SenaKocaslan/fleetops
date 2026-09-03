using FleetOps.SharedKernel;

namespace FleetOps.Tasks.Application;

public sealed record StartTaskCommand(Guid TaskId) : ICommand;

public sealed record CompleteTaskCommand(Guid TaskId) : ICommand;
