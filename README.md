# FleetOps

[![CI](https://github.com/SenaKocaslan/fleetops/actions/workflows/ci.yml/badge.svg)](https://github.com/SenaKocaslan/fleetops/actions/workflows/ci.yml)

AGV (Automated Guided Vehicle) filo ve gorev yonetim sistemi.

Fabrika/depo icinde calisan AGV filosunun merkezi yonetimi: gorev havuzu ve atama,
dar koridor/kapi gibi paylasilan kaynaklarin kilitlenmesi, tamamlanan gorevin stok
hareketine donusmesi ve filo durumunun canli izlenmesi.

> **Kapsam disi:** navigasyon, rota planlama, engelden kacinma, motor kontrolu.
> Bunlar aracin kendi yaziliminda kalir. Sistem "nereye gidilecegini" soyler,
> "nasil gidilecegini" degil.



## Mimari


![Modular monolith](img/modular-monolith.png)

### Cozum yapisi

![Cozum yapisi](img/cozum-yapisi.png)

### Modul ic mimarisi

![Modul ic mimarisi](img/modul-ic-mimarisi.png)

### Teknoloji haritasi

![Teknoloji haritasi](img/teknoloji-haritasi.png)

### Calisma ani akislari

![Calisma ani akislari](img/calisma-ani-akislari.png)

---

**Modular Monolith** — tek deploy birimi, uc bagimsiz modul:

| Modul | Sorumluluk |
|---|---|
| `Fleet` | AGV kayit, durum, batarya |
| `Tasks` | Gorev havuzu, atama, kaynak kilidi, outbox |
| `Stock` | Lokasyon, malzeme hareketi |

Moduller birbirinin `DbContext`'ini gormez, aralarinda foreign key yoktur,
birbirlerinin handler'larini cagirmaz ve birbirlerine proje referansi vermez.

Iletisim iki kanaldan olur:

| Kanal | Ne zaman | Ornek |
|---|---|---|
| Integration event (asenkron) | Sonuc simdi lazim degilse | Gorev tamamlandi -> stok hareketi olussun |
| `SharedKernel` arayuzu (senkron) | Cevap KARAR ANINDA lazimsa | Bu AGV su anda gorev alabilir mi? |

Ikinci kanal bilincli bir esneme. "Bu arac gorev alabilir mi" sorusunun cevabi
atama karari verilirken lazim; olay uzerinden beslenen bir kopya tablo dogasi
geregi bir tik geride olur ve tam da karar aninda yanlis cevap verir. Kural
yine korunuyor: soran modul, cevaplayan modulu referans vermiyor -- arada
`SharedKernel`'deki arayuz ve notr bir kayit duruyor.

### Uygulanan pattern'ler

| Pattern | Cozdugu problem |
|---|---|
| Modular Monolith | Stok mantigi ile filo mantigi karismasin |
| CQRS | Yazma aggregate'ten, okuma projeksiyondan |
| Aggregate + rich domain | Gecersiz durum gecisi imkansiz olsun |
| Repository | Aggregate bir butun olarak yuklensin/kaydedilsin |
| Optimistic Concurrency | Gorev/kilit iki kez verilmesin |
| State Machine | Gecisler tek yerde tanimli olsun |
| Strategy | Otomatik atamada arac secim kurali degisebilir olsun |
| Decorator | Log ve transaction handler'i kirletmesin |
| Outbox | Kayit gitti ama olay gitmedi durumu olmasin |
| Dead Letter | Bozuk bir olay kuyrugu sonsuza kadar mesgul etmesin |
| Integration Events | Moduller birbirini dogrudan cagirmasin |
| Hosted Service | Takili kilitler serbest kalsin, bataryasi biten arac sarja gitsin |
| Options | Ayarlar koda gomulmesin |

## Teknoloji

| Katman | Teknoloji |
|---|---|
| Runtime | .NET 10 |
| Web/API | ASP.NET Core (Minimal API) |
| Arayuz | Angular 22 (standalone bilesenler + signals) |
| ORM | EF Core 10 |
| Veritabani | PostgreSQL |
| Canli veri | SignalR |
| Kimlik | JWT (HMAC-SHA256) + politika bazli yetkilendirme, parolalar PBKDF2 |
| Test | xUnit + Testcontainers (backend), Vitest (birim), Playwright (e2e) |
| Dagitim | Docker + Docker Compose (nginx arkasinda tek origin) |

## Calistirma

### Docker ile (tam yigin)

```bash
cp .env.example .env
# .env icindeki JWT_SIGNING_KEY'i degistirin:
#   openssl rand -base64 48
docker compose up -d
```

| Adres | Ne |
|---|---|
| http://localhost:8080 | Arayuz (nginx; /api ve /hubs API'ye vekillenir) |
| http://localhost:5200 | API, dogrudan (yalnizca hata ayiklama) |
| localhost:55432 | PostgreSQL |

Giris: `supervisor` / `Supervisor123!` veya `operator` / `Operator123!`.
Bunlar tohum verisi; gercek kurulumda silinmeleri gerekir.

Servisler kosula bagli sirayla kalkar:

```
db (healthy)  ->  migrate (exit 0)  ->  api (healthy)  ->  web
```

**`/health` veritabanina bakar.** Veritabanina ulasilamazsa 503 doner;
Docker HEALTHCHECK, compose'daki baslatma sirasi ve CI bu cevaba guveniyor.
Veritabani geri geldiginde API yeniden baslatilmadan kendiliginden 200'e
doner. CI her push'ta veritabanini durdurup bunu yeniden sinar.

**Migration acilista calismaz.** Ayri bir servis (`migrate`) `--migrate`
argumaniyla bir kez calisip cikar. Sebebi: `api` birden fazla kopya olarak
kalkarsa her kopya ayni semayi ayni anda degistirmeye calisirdi. Ayrica
uygulamanin kendisinin sema degistirme yetkisine ihtiyaci kalmaz.

Sifirdan olcum: bos volume uzerinde `docker compose up -d` -> tum yigin
**11 saniyede** hazir, tohum verisi (3 AGV, 4 lokasyon, 3 kaynak, 2 kullanici)
migration'lardan gelir.

### Gelistirme ortami

```bash
docker compose up -d db                    # yalnizca veritabani

cd backend
dotnet run --project src/FleetOps.Api -- --migrate   # dort semayi da gocurur, cikar
dotnet run --project src/FleetOps.Api               # http://localhost:5199

cd ../frontend
npm ci && npm start                        # http://localhost:4200
```

## Test

```bash
cd backend  && dotnet test                 # 109 birim + 137 integration
cd frontend && npm test                    # 28 birim (Vitest)
cd frontend && npm run e2e                 # 31 uctan uca (Playwright)
```

Integration testler Testcontainers ile **gercek PostgreSQL 17** ayaga kaldirir;
in-memory saglayici kullanilmaz. Sebebi: bu projedeki kritik davranislarin cogu
(xmin optimistic concurrency, kismi tekil indeks, snake_case, enum'un metin
olarak saklanmasi) in-memory saglayicida hic calismaz ve test yanlis yere
yesil yanar.

Her push'ta ayni testler GitHub Actions uzerinde de kosuyor (dort is:
backend, arayuz birim, uctan uca, dagitim yigini). Dagitim isi imajlari
derlemekle kalmiyor, bos bir veritabaninda tum zinciri kaldirip giris
akisindan geciyor - "tek komutla ayaga kalkar" iddiasi her push'ta
yeniden dogrulaniyor.

E2E kosmadan once `docker compose stop api` yapin: konteynerdeki simulator
ayni veritabanina telemetri yazar ve testlerin altindan AGV durumunu kaydirir.

## Otomatik sarj

Bataryasi esigin altina dusen **musait** arac sarja alinir, sarji yeterli
seviyeye gelen arac servise doner (`SarjYonlendirici`, periyodik servis).

**Mesgul arac sarja gonderilmez:** yurutulen gorev yarida kalir ve havuza
donmedigi icin kimse fark etmezdi. Arac gorevi bitirip Available'a dondugunde
bir sonraki tur onu alir.

**Iki esik var, tek esik degil** (varsayilan 25 ve 80). Tek esik olsaydi tam o
degerdeki arac her turda sarja gir/cik yapardi: sarja alinir, bir tik sarj
olur, esigi gecer, servise alinir, gorev alir, tekrar duser. Aradaki bosluk
bu salinimi kesiyor.

**Karar telemetride degil.** Telemetri aracin bildirimi, bir karar degil;
"bataryasi dusen arac sarja gitsin" ise bir filo politikasi ve zamanla
degisebilir. Telemetriye gomulseydi her olcum bir karar noktasi olurdu.

Sarjdaki ya da servis disi araca **gorev atanamaz** ve **kaynak kilidi
verilmez**. Mesgul arac kilit ALABILIR: kilit zaten gorev yurutulurken
alinir.

## Otomatik atama

Havuzdaki bekleyen gorevler, oncelik sirasina gore musait araclara dagitilir
(`POST /api/dispatch/auto-assign`, yalnizca Supervisor). Arac secim kurali
`IAtamaStratejisi` arkasinda; varsayilan kural bataryasi en yuksek araci
seciyor, esitlikte arac koduna gore sabit bir sira uyguluyor.

Dagitim **composition root'ta** duruyor, bir modulun icinde degil: "hangi
goreve hangi arac" sorusu iki modulun verisini birden gerektiriyor ve Tasks,
Fleet'in araclarini goremiyor. Alarmlarin birlestirildigi yerle ayni gerekce.

Bir tur icinde ayni araca iki gorev verilmemesi yerel bir aday listesiyle
saglaniyor; sebebi, Fleet'in aracin mesgullestigini ancak outbox olayi teslim
edildikten SONRA ogrenmesi. Son bekci ise veritabani: `task_assignment`
uzerindeki kismi tekil indeks, bir AGV'nin ayni anda birden fazla acik
atamasi olmasini reddediyor.

**Kapsam disi:** koridor/kapi kilidini otomatik almak. Bir gorevin hangi
kaynaklardan gececegi bilgisi sistemde yok (rota, aracin kendi yaziliminda);
bu veri olmadan otomatik kilit, sistemi oldugundan akilli gostermek olurdu.

## Guvenlik notlari

- **JWT imza anahtari koda gomulu degil.** Uretimde `JWT_SIGNING_KEY` ortam
  degiskeninden gelir; `.env` gitignore'da. Anahtar 32 bayttan kisaysa uygulama
  **acilista** hata verir - sessizce sahte token uretmesindense patlamasi dogru.
- `appsettings.Development.json` icindeki anahtar yalnizca yerel gelistirme
  icindir ve `.dockerignore` ile **imaja alinmaz**.
- Parolalar PBKDF2-SHA256, 100.000 tur, kullanici basina rastgele tuz ile
  saklanir; dogrulama sabit zamanli karsilastirma yapar.
- Bilinmeyen kullanici ile yanlis parola **ayni** yaniti doner; farkli yanit
  hangi kullanici adlarinin var oldugunu sizdirirdi.
- Konteynerler root olmayan kullaniciyla calisir (`uid=1654 app`).
- Bir uc noktaya `RequireAuthorization` eklemeyi unutmak sessiz bir aciktir;
  bir test tum uc noktalari sayip denetler, beyaz liste yalnizca
  `/api/auth/login`.
