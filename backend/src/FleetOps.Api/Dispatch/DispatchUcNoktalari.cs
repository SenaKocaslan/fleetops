using FleetOps.SharedKernel;

namespace FleetOps.Api.Dispatch;

public static class DispatchUcNoktalari
{
    public static IServiceCollection AddDispatch(this IServiceCollection services)
    {
        services.AddScoped<IAtamaStratejisi, EnYuksekBataryaStratejisi>();
        services.AddScoped<OtomatikAtamaServisi>();

        return services;
    }

    public static void MapDispatchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // "/api/tasks" altina degil ayri bir gruba konuldu: bu uc nokta iki
        // modulun verisini birden kullaniyor, tek bir modulun ucu degil.
        endpoints.MapPost("/api/dispatch/auto-assign", async (
            OtomatikAtamaServisi servis,
            CancellationToken ct) =>
        {
            var sonuc = await servis.CalistirAsync(ct);

            return sonuc.IsSuccess
                ? Results.Ok(sonuc.Value)
                : Results.BadRequest(new { code = sonuc.Error.Code, message = sonuc.Error.Message });
        }).WithTags("Dispatch").RequireAuthorization(Politikalar.GorevPlanlama);
    }
}
