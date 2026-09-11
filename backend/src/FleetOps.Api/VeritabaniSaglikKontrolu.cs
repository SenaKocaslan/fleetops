using FleetOps.Api.Auth;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FleetOps.Api;

// /health onceden yalnizca surecin ayakta oldugunu soyluyordu. Olculdu
// (2026-09-11): makine yeniden basladiginda db konteyneri kalkmadi, /health
// 200 dondu, giris 500. Compose zinciri, Docker HEALTHCHECK ve CI bu uc
// noktaya guveniyor; veritabanina bakmayan bir saglik kontrolu onlara
// yanlis bilgi veriyordu.
//
// Auth semasinin baglami kullaniliyor, cunku Api modullerin DbContext'lerini
// tanimiyor (mimari denetimde kontrol edilen kural). Hepsi ayni veritabanina
// bagli; tek bir baglanti yeterli kanit.
//
// CanConnectAsync havuzdan onceden acilmis bir baglanti alip "baglandim"
// diyebilir mi diye olculdu: havuz isinmisken db durduruldu, kontrol yine
// 503 dondu. Ek bir sorguya gerek yok.
public sealed class VeritabaniSaglikKontrolu(AuthDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Veritabanina ulasilamiyor.");
}
