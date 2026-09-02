using FleetOps.SharedKernel;

namespace FleetOps.Tasks.Application;

public sealed record CreateTaskCommand(
    Guid FromLocationId,
    Guid ToLocationId,
    string MaterialCode,
    int Quantity,
    int Priority) : ICommand<Guid>;
