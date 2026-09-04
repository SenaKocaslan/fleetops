using Microsoft.AspNetCore.SignalR;

namespace FleetOps.Fleet.Infrastructure;

// Istemciden sunucuya cagrilan metot yok: akis tek yonlu (sunucu -> istemci).
// Bos govde bilerek; hub yalnizca baglanti ve yayin kanali.
public sealed class FleetHub : Hub
{
    public const string Yol = "/hubs/fleet";

    public const string AgvDegisti = "agvDegisti";
}
