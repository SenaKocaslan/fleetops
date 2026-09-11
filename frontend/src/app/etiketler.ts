// Sunucu durumlari Ingilizce enum adlariyla gonderiyor (Pending, Busy...).
// Arayuzde Turkce gosteriliyor; ham deger data-durum ozniteliginde kaliyor ki
// testler ve renkler gorunen metne bagli olmasin.

const GOREV_DURUMLARI: Record<string, string> = {
  Pending: 'Bekliyor',
  Assigned: 'Atandı',
  InProgress: 'Yürütülüyor',
  Completed: 'Tamamlandı',
  Failed: 'Başarısız',
  Cancelled: 'İptal edildi',
};

const ARAC_DURUMLARI: Record<string, string> = {
  Available: 'Müsait',
  Busy: 'Meşgul',
  Charging: 'Şarjda',
  OutOfService: 'Servis dışı',
};

const SIDDETLER: Record<string, string> = {
  Kritik: 'Kritik',
  Uyari: 'Uyarı',
  Bilgi: 'Bilgi',
};

const ROLLER: Record<string, string> = {
  Supervisor: 'Süpervizör',
  Operator: 'Operatör',
};

const KAYNAK_TURLERI: Record<string, string> = {
  ChargingDock: 'Şarj istasyonu',
  Corridor: 'Koridor',
  Lift: 'Asansör',
};

// Alarm kodu makine icin (Tasks.UzunSureBekleyenGorev); kullaniciya bir ad
// gosteriliyor, kod ise yaninda kucuk yaziliyor.
const ALARM_ADLARI: Record<string, string> = {
  'Fleet.KritikBatarya': 'Kritik batarya',
  'Fleet.DusukBatarya': 'Düşük batarya',
  'Fleet.TelemetriKesildi': 'Telemetri kesildi',
  'Tasks.UzunSureBekleyenGorev': 'Uzun süredir bekleyen görev',
  'Tasks.BaslamayanGorev': 'Başlamayan görev',
  'Tasks.TakiliKilit': 'Takılı kalan kilit',
  'Tasks.TeslimEdilemeyenOlay': 'Teslim edilemeyen olay',
  'Stock.EksiBakiye': 'Eksi stok bakiyesi',
};

// Bilinmeyen bir deger gelirse ham hali gosterilir: sunucuya yeni bir durum
// eklendiginde arayuz bos kalmasin.
const cevir = (tablo: Record<string, string>) => (deger: string | null | undefined) =>
  deger ? (tablo[deger] ?? deger) : '';

export const gorevDurumu = cevir(GOREV_DURUMLARI);
export const aracDurumu = cevir(ARAC_DURUMLARI);
export const siddet = cevir(SIDDETLER);
export const rol = cevir(ROLLER);
export const kaynakTuru = cevir(KAYNAK_TURLERI);
export const alarmAdi = cevir(ALARM_ADLARI);
