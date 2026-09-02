using FleetOps.SharedKernel.Domain;

namespace FleetOps.SharedKernel;

/// <summary>Sistemin durumunu degistiren istek. Sonuc dondurmez.</summary>
public interface ICommand;

/// <summary>Sistemin durumunu degistiren ve bir deger donduren istek.</summary>
public interface ICommand<TResponse>;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
