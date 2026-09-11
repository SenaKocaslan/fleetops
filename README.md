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

**Islenmis outbox mesajlari 7 gun sonra silinir** (`OutboxTemizleyici`).
Teslim edildikten sonra ise yaramiyorlar; bu sure yalnizca "olay gercekten
gitti mi" sorusuna bakabilmek icin. **Olu mektuplara dokunulmaz:** onlar
insan mudahalesi bekliyor, silinirlerse alarm da kaybolur.

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

## API dokumani

OpenAPI 3.1 dokumani **yalnizca gelistirme ortaminda** yayinlanir:

```bash
dotnet run --project backend/src/FleetOps.Api
curl http://localhost:5199/openapi/v1.json
```

Uretimde (Docker yigini) kapali: API haritasini herkese acmak gereksiz bir
bilgi sizintisi olurdu. Dokuman Postman/Insomnia gibi araclara dogrudan
yuklenebilir.

Her uc noktada token gerekip gerekmedigi ve **hangi rolun cagirabildigi**
yazili. Roller elle yazilmiyor, politikalarin gercek tanimindan okunuyor;
`AuthKurulumu`'nda bir rol degisirse dokuman da degisir. Bir test, her
`/api` uc noktasinin dokumanda bulundugunu gercek uc nokta listesiyle
karsilastirarak dogruluyor.

Yanit govdeleri dokumanda tipli degil (uc noktalar `IResult` donuyor);
yollar, parametreler, istek govdeleri ve yetki bilgisi eksiksiz.

## Test

```bash
cd backend  && dotnet test                 # 119 birim + 166 integration
cd frontend && npm test                    # 31 birim (Vitest)
cd frontend && npm run e2e                 # 35 uctan uca (Playwright)
cd frontend && npm run e2e:tip             # e2e dosyalarinin tip denetimi
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

## Alarmlar

Her modul kendi alarmlarini uretir (`IAlarmSource`); birlestirme composition
root'ta. Alarmlar tabloya yazilmaz, mevcut veriden hesaplanir.

**Ture gore gruplu.** Olculdu: canli sistemde 51 alarm satirinin 51'i ayni
turdendi ("uzun sure bekleyen gorev") ve operator ayni seyi soyleyen 51
satira bakiyordu. Her turden en fazla 5 ornek doner, geri kalani sayi olarak
(`groups[].count`). Tur icindeki sira kaynaktan geldigi gibi korunur (ornegin
en uzun bekleyen gorev once). Kritik alarm rozeti kesilmeden, gercek sayi
uzerinden hesaplanir.

## Stok bakiyesi

Raf bazinda anlik stok, hareketlerden hesaplanir (`GET /api/stock/balances`);
ayri bir bakiye tablosu tutulmaz, ayni bilgi iki yerde durup ayrismasin diye.
Sorgu tamamen veritabaninda calisir: giris ve cikislar `UNION ALL`, sonra
`GROUP BY` ve `HAVING <> 0`.

**Yalnizca Depo bolgesi.** Sistem depo ICI tasimayi izliyor; tedarikciden
kabul alanina gelen ve sevkiyattan musteriye giden malzeme sisteme girmiyor.
Bu iki bolge bir sinir ve oradaki bakiye hareketlerden hesaplanamaz. Olculdu:
KABUL-01 -182 cikiyordu.

Rafta **eksi bakiye** kayit ile gercegin ayristigini gosterir (rafa sistem
disinda malzeme gelmis ya da kaydi olmayan bir cikis yapilmis) ve
`Stock.EksiBakiye` alarmi uretir.

## Gorev yasam dongusu

| Islem | Gecis | Kim | Arac |
|---|---|---|---|
| Havuza dondur | Assigned -> Pending | Supervisor | serbest kalir |
| Basarisiz | InProgress -> Failed | Operator, Supervisor | serbest kalir |
| Iptal | Pending -> Cancelled | Supervisor | (atanmamis) |

Atanmis gorev dogrudan iptal edilemez; once havuza donmeli. "Havuza dondur",
"atandi ama baslamadi" alarminin cozumu.

Havuza dondurme ve basarisizlik `TaskAssignmentEnded` olayini yayinlar; Fleet
bu olayla araci serbest birakir. Onceden bu iki gecis hic olay
yayinlamiyordu: uc noktalari eklenseydi bile arac Fleet'te sonsuza kadar
Busy kalir ve filodan sessizce duserdi.

**Fleet'in olay tuketicileri idempotent.** Teslimat en az bir kez; ayni olay
iki kez gelebilir. Olculdu: onceki gorevin tekrar gelen bitis olayi, araci
yeni gorevinin ortasinda serbest birakiyordu. Stock'taki kalip Fleet'e de
uygulandi: olay kimligi `fleet.processed_integration_event` tablosunun
birincil anahtari, durum degisikligiyle ayni transaction'da yaziliyor.

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

Gorev onceligi 1-10 araligindadir (buyuk sayi daha oncelikli); sinir olmadiginda
0 ve negatif degerler sessizce kaybolan gorevler uretiyordu.

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
- **Veritabani ve API portlari yalnizca `127.0.0.1`'e bagli.** Olculdu:
  onceden `0.0.0.0`'a bagliydi ve makinenin ag adresinden varsayilan
  parolayla veritabanina baglanilabiliyordu. Agdan gelen herkes yalnizca
  arayuze (8080) ulasir ve nginx'ten gecer.
- **Giris sure sizdirmaz.** Olculdu: var olan kullaniciya yanlis parola
  63 ms, olmayan kullanici 1 ms suruyordu; yanit ayniydi ama sure hangi
  kullanici adlarinin var oldugunu sizdiriyordu. Kullanici yoksa da ayni
  maliyette bir dogrulama yapiliyor.
- **Giris denemeleri iki katmanda sinirli.** Kullanici adi basina 5
  basarisiz denemede 5 dakika kilit (uygulamada; kilit olmayan kullanici
  adlari icin de isler, yoksa 429/401 farki var olani sizdirirdi). IP basina
  dakikada 10 deneme (nginx'te; uygulama nginx'in arkasinda oldugu icin
  gercek istemci IP'sini goremez).
- nginx `api` adini her istekte Docker DNS'inden yeniden cozer. Olculdu:
  sabit adla yazildiginda nginx IP'yi yalnizca acilista cozuyordu; api
  konteyneri yeniden olusturulup yeni IP aldiginda arayuzun tum API
  cagrilari 502 dondu. CI bunu her push'ta deterministik olarak sinar.
- Bilinen bedel: saldirgan baskasinin kullanici adina yanlis parola
  deneyerek onu 5 dakika kilitleyebilir. Compose'daki varsayilan veritabani
  parolasi yalnizca yerel erisim icin; uretimde `.env` ile degistirilmeli.
- Bir uc noktaya `RequireAuthorization` eklemeyi unutmak sessiz bir aciktir;
  bir test tum uc noktalari sayip denetler, beyaz liste yalnizca
  `/api/auth/login`.
