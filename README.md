# FleetOps

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

Moduller birbirinin `DbContext`'ini gormez, aralarinda foreign key yoktur ve
birbirlerinin handler'larini cagirmazlar. Iletisim yalnizca integration event ile olur.

### Uygulanan pattern'ler

| Pattern | Cozdugu problem |
|---|---|
| Modular Monolith | Stok mantigi ile filo mantigi karismasin |
| CQRS | Yazma aggregate'ten, okuma projeksiyondan |
| Aggregate + rich domain | Gecersiz durum gecisi imkansiz olsun |
| Repository | Aggregate bir butun olarak yuklensin/kaydedilsin |
| Optimistic Concurrency | Gorev/kilit iki kez verilmesin |
| State Machine | Gecisler tek yerde tanimli olsun |
| Strategy | Atama kurali degisebilir olsun |
| Decorator | Log ve transaction handler'i kirletmesin |
| Outbox | Kayit gitti ama olay gitmedi durumu olmasin |
| Integration Events | Moduller birbirini dogrudan cagirmasin |
| Hosted Service | Takili kilitler serbest kalsin |
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
cd backend  && dotnet test                 # 107 birim + 105 integration
cd frontend && npm test                    # 25 birim (Vitest)
cd frontend && npm run e2e                 # 28 uctan uca (Playwright)
```

Integration testler Testcontainers ile **gercek PostgreSQL 17** ayaga kaldirir;
in-memory saglayici kullanilmaz. Sebebi: bu projedeki kritik davranislarin cogu
(xmin optimistic concurrency, kismi tekil indeks, snake_case, enum'un metin
olarak saklanmasi) in-memory saglayicida hic calismaz ve test yanlis yere
yesil yanar.

E2E kosmadan once `docker compose stop api` yapin: konteynerdeki simulator
ayni veritabanina telemetri yazar ve testlerin altindan AGV durumunu kaydirir.

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
