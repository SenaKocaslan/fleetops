namespace FleetOps.SharedKernel;

public enum AlarmSeverity
{
    Bilgi = 0,
    Uyari = 1,
    Kritik = 2,
}

public sealed record AlarmSummary(
    string Code,
    AlarmSeverity Severity,
    string Subject,
    string Message,
    DateTime DetectedAtUtc);

// Her modul KENDI alarmlarini uretir; hicbiri digerinin verisini gormez.
// Toplama isi composition root'a ait: moduller birbirini cagiramadigi icin
// "tum alarmlar" sorusunun cevabi ancak orada birlestirilebilir.
//
// Alarmlar TABLOYA YAZILMIYOR: mevcut veriden hesaplaniyor. Kalici tablo,
// birinin alarmi "gordum" diye isaretlemesi gerektiginde anlamli olur;
// bugun oyle bir ihtiyac yok, tablo eklemek durumu iki yerde tutmak olurdu.
public interface IAlarmSource
{
    Task<IReadOnlyList<AlarmSummary>> AlarmlariGetirAsync(CancellationToken cancellationToken);
}
