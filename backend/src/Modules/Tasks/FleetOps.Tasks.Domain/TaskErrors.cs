using FleetOps.SharedKernel.Domain;

namespace FleetOps.Tasks.Domain;

public static class TaskErrors
{
    public static readonly Error MalzemeKoduBos =
        new("Task.MalzemeKoduBos", "Malzeme kodu bos olamaz.");

    public static readonly Error MiktarPozitifOlmali =
        new("Task.MiktarPozitifOlmali", "Miktar sifirdan buyuk olmalidir.");

    public static readonly Error AyniLokasyon =
        new("Task.AyniLokasyon", "Kaynak ve hedef lokasyon ayni olamaz.");

    public static readonly Error LokasyonBos =
        new("Task.LokasyonBos", "Kaynak ve hedef lokasyon zorunludur.");

    public static readonly Error AgvBos =
        new("Task.AgvBos", "Atanacak AGV kimligi bos olamaz.");

    public static readonly Error Bulunamadi =
        new("Task.Bulunamadi", "Gorev bulunamadi.");

    // Ayni goreve iki istek ayni anda yazmaya calisti; biri kaybetti.
    // Isteyen tekrar deneyebilir - bu bir hata degil, bir yaris sonucu.
    public static readonly Error EszamanliDegisiklik =
        new("Task.EszamanliDegisiklik",
            "Gorev bu sirada baska bir istek tarafindan degistirildi. Lutfen tekrar deneyin.");

    public static Error GecersizGecis(TransportTaskStatus mevcut, TransportTaskStatus hedef) =>
        new("Task.GecersizGecis", $"Gorev durumu '{mevcut}' iken '{hedef}' durumuna gecemez.");
}
