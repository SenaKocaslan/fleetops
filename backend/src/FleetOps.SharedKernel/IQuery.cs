using FleetOps.SharedKernel.Domain;

namespace FleetOps.SharedKernel;

/// <summary>
/// Durumu degistirmeyen okuma istegi. Aggregate yuklemez, dogrudan
/// projeksiyon dondurur - liste ekrani icin tum is kurallarini bellege
/// almak gereksizdir.
/// </summary>
public interface IQuery<TResponse>;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
