using FleetOps.SharedKernel.Domain;

namespace FleetOps.Tasks.Domain;

public static class TaskErrors
{
    public static readonly Error MalzemeKoduBos =
        new("Task.MalzemeKoduBos", "Malzeme kodu boş olamaz.");

    public static readonly Error MiktarPozitifOlmali =
        new("Task.MiktarPozitifOlmali", "Miktar sıfırdan büyük olmalıdır.");

    public static readonly Error OncelikAraligiDisi =
        new("Task.OncelikAraligiDisi",
            $"Öncelik {TransportTask.AsgariOncelik} ile {TransportTask.AzamiOncelik} arasında olmalıdır.");

    public static readonly Error AyniLokasyon =
        new("Task.AyniLokasyon", "Kaynak ve hedef lokasyon aynı olamaz.");

    public static readonly Error LokasyonBos =
        new("Task.LokasyonBos", "Kaynak ve hedef lokasyon zorunludur.");

    public static readonly Error AgvBos =
        new("Task.AgvBos", "Atanacak AGV kimliği boş olamaz.");

    // Bir AGV ayni anda tek bir tasima gorevi yurutur. Istemci yanlis bir sey
    // yapmadi; arac serbest kalinca ayni istek gecerli olacak.
    public static readonly Error AgvMesgul =
        new("Task.AgvMesgul", "Bu AGV'nin hâlihazırda açık bir görevi var.");

    public static readonly Error Bulunamadi =
        new("Task.Bulunamadi", "Görev bulunamadı.");

    public static readonly Error EszamanliDegisiklik =
        new("Task.EszamanliDegisiklik",
            "Görev bu sırada başka bir istek tarafından değiştirildi. Lütfen tekrar deneyin.");

    // Arac sarjda, servis disi ya da bataryasi yetersiz. Istemci yanlis bir
    // sey yapmadi; arac uygun hale gelince ayni istek gecerli olacak.
    public static Error AgvGorevAlamaz(string kod) =>
        new("Task.AgvGorevAlamaz", $"{kod} şu anda görev alamaz (şarjda, servis dışı veya bataryası yetersiz).");

    public static readonly Error AgvBulunamadi =
        new("Task.AgvBulunamadi", "Atanacak AGV bulunamadı.");

    public static Error GecersizGecis(TransportTaskStatus mevcut, TransportTaskStatus hedef) =>
        new("Task.GecersizGecis", $"Görev \"{DurumAdi(mevcut)}\" durumundayken \"{DurumAdi(hedef)}\" durumuna geçemez.");

    // Mesaj kullaniciya aynen gosteriliyor; ham enum adi ("Assigned") Turkce
    // bir cumlenin icinde yabanci kalirdi.
    private static string DurumAdi(TransportTaskStatus durum) => durum switch
    {
        TransportTaskStatus.Pending => "Bekliyor",
        TransportTaskStatus.Assigned => "Atandı",
        TransportTaskStatus.InProgress => "Yürütülüyor",
        TransportTaskStatus.Completed => "Tamamlandı",
        TransportTaskStatus.Failed => "Başarısız",
        TransportTaskStatus.Cancelled => "İptal edildi",
        _ => durum.ToString(),
    };
}
