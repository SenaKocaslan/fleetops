# FleetOps — Functional Analysis

## 1. Modül Haritası

```mermaid
flowchart TB
    subgraph host["Host — ASP.NET Core"]
        direction TB
        subgraph mods[" "]
            direction LR
            F["<b>Fleet</b><br/>AGV kayit, durum, batarya"]
            T["<b>Tasks</b><br/>gorev havuzu, atama,<br/>kaynak kilidi"]
            S["<b>Stock</b><br/>lokasyon, malzeme hareketi"]
        end
        SK["<b>SharedKernel</b><br/>ICommand IQuery Result&lt;T&gt;<br/>IntegrationEvent Outbox"]
    end

    T -.->|"TaskCompleted<br/><i>integration event</i>"| S
    T -.->|"AgvAssigned"| F

    F --> SK
    T --> SK
    S --> SK

    style mods fill:none,stroke:none
    style host fill:#f5f5f5,stroke:#999999
    style T fill:#d5e8d4,stroke:#82b366
    style SK fill:#fff2cc,stroke:#d6b656
```

### Modül kuralları

| Kural | Gerekçe |
|---|---|
| Modüller birbirinin `DbContext`'ini görmez | Her modülün kendi şeması ve migration'ı var |
| Modüller arası **foreign key yok** | Sadece ID ile referans; sınır veritabanı seviyesinde de korunur |
| Modüller birbirinin handler'ını çağırmaz | İletişim yalnızca integration event ile |
| Ortak olan her şey `SharedKernel`'de | Ama iş mantığı asla orada değil |

`Tasks` yeşil çünkü sistemin ağırlık merkezi orada: aggregate, concurrency,
kaynak kilidi ve outbox hep bu modülde.

## 2. Modül İçi Katmanlar

```mermaid
flowchart TB
    E["<b>Endpoints</b><br/>minimal API"]
    A["<b>Application</b><br/>command/query handler, DTO"]
    D["<b>Domain</b><br/>aggregate, kurallar, domain event"]
    I["<b>Infrastructure</b><br/>DbContext, repository, EF konfigurasyon"]

    E --> A
    A --> D
    I -.->|"arayuzleri uygular"| A
    I --> D

    style D fill:#ffe6cc,stroke:#d79b00
    style I fill:#f8cecc,stroke:#b85450
```

Her modül kendi içinde bu dört katmanı taşır. Bağımlılık yönü GameStore'daki ile aynı:
`Infrastructure` yukarı bakar, `Domain` hiçbir şeye bakmaz.

## 3. Aktörler ve Use Case'ler

```mermaid
flowchart LR
    OP(["Operator"])
    SV(["Supervisor"])
    AGV(["AGV"])

    subgraph sistem["FleetOps"]
        UC1["UC-1<br/>Gorev olustur"]
        UC2["UC-2<br/>Gorev talep et"]
        UC3["UC-3<br/>Kaynak kilidi al"]
        UC4["UC-4<br/>Gorev tamamla"]
        UC5["UC-5<br/>Filoyu izle"]
        UC6["UC-6<br/>AGV kaydet"]
        UC7["UC-7<br/>Gecmisi raporla"]
    end

    OP --> UC1
    OP --> UC5
    SV --> UC6
    SV --> UC7
    AGV --> UC2
    AGV --> UC3
    AGV --> UC4

    style sistem fill:#f5f5f5,stroke:#999999
    style UC2 fill:#d5e8d4,stroke:#82b366
    style UC3 fill:#d5e8d4,stroke:#82b366
```

Yeşil iki use case sistemin zor kısmıdır — ikisi de concurrency içerir.

| Aktör | Tanım |
|---|---|
| Operator | Görev oluşturur, filoyu izler |
| Supervisor | AGV kaydeder, politika ayarlar, rapor alır |
| AGV | Sistem aktörü; görev ister, kilit ister, durum bildirir |

## 4. Domain Sözlüğü

| Terim | Tanım |
|---|---|
| **Agv** | Filodaki bir araç. Durumu ve batarya seviyesi vardır |
| **TransportTask** | Bir noktadan diğerine malzeme taşıma işi. **Aggregate root** |
| **TaskAssignment** | Bir görevin bir AGV'ye atanması. Aggregate içinde yaşar |
| **Resource** | Aynı anda tek AGV'nin kullanabileceği fiziksel alan (koridor, kapı, asansör) |
| **ResourceLock** | Bir kaynağın bir AGV tarafından tutulması. Süre aşımına uğrar |
| **Location** | Alma/bırakma noktası (raf, istasyon) |
| **StockMovement** | Malzemenin bir lokasyondan diğerine geçtiğinin kaydı |
| **AssignmentPolicy** | Görevin hangi AGV'ye verileceğini belirleyen kural (**Strategy**) |

## 5. Veri Modeli

```mermaid
erDiagram
    AGV {
        guid id PK
        string code
        string status
        int battery_level
        guid current_location_id "FK yok - modul disi"
        bytea row_version
    }

    TRANSPORT_TASK {
        guid id PK
        string status
        guid from_location_id "FK yok - modul disi"
        guid to_location_id "FK yok - modul disi"
        string material_code
        int quantity
        int priority
        timestamp created_at
        bytea row_version
    }
    TASK_ASSIGNMENT {
        guid id PK
        guid task_id FK
        guid agv_id "FK yok - modul disi"
        timestamp assigned_at
        timestamp completed_at
    }
    RESOURCE {
        guid id PK
        string code
        string kind
    }
    RESOURCE_LOCK {
        guid id PK
        guid resource_id FK
        guid agv_id "FK yok - modul disi"
        timestamp acquired_at
        timestamp expires_at
        bytea row_version
    }
    OUTBOX_MESSAGE {
        guid id PK
        string type
        jsonb payload
        timestamp occurred_at
        timestamp processed_at
    }

    LOCATION {
        guid id PK
        string code
        string zone
    }
    STOCK_MOVEMENT {
        guid id PK
        string material_code
        int quantity
        guid from_location_id FK
        guid to_location_id FK
        guid source_task_id "FK yok - modul disi"
        timestamp moved_at
    }

    TRANSPORT_TASK ||--o{ TASK_ASSIGNMENT : "atanir"
    RESOURCE ||--o{ RESOURCE_LOCK : "kilitlenir"
    LOCATION ||--o{ STOCK_MOVEMENT : "kaynak/hedef"
```

### Modül sahipliği

| Modül | Tablolar |
|---|---|
| **Fleet** | `AGV` |
| **Tasks** | `TRANSPORT_TASK`, `TASK_ASSIGNMENT`, `RESOURCE`, `RESOURCE_LOCK`, `OUTBOX_MESSAGE` |
| **Stock** | `LOCATION`, `STOCK_MOVEMENT` |

### Modeldeki dört karar

**`row_version` neden var?**
İki AGV aynı görevi aynı anda isteyebilir. `row_version` (PostgreSQL'de `xmin` veya
`bytea` concurrency token) sayesinde ikinci güncelleme çakışma hatası alır. Bu, projenin
en kritik garantisi.

**Modüller arası neden foreign key yok?**
`TRANSPORT_TASK.from_location_id` bir `LOCATION`'a işaret eder ama FK constraint yoktur.
Sebep: FK koyarsak `Tasks` ve `Stock` aynı şemayı paylaşmak zorunda kalır ve modül sınırı
veritabanı seviyesinde çöker. Referans yalnızca ID ile taşınır, tutarlılık uygulama
katmanında korunur.

**`OUTBOX_MESSAGE` neden `Tasks` modülünde?**
Outbox, olayı **üreten** modülün transaction'ına ait olmalı. Görev tamamlanma kaydı ile
"bunu duyur" niyeti aynı transaction'da yazılır.

**`TASK_ASSIGNMENT` neden ayrı tablo?**
Bir görev reddedilip yeniden atanabilir. Atama geçmişi verimlilik raporunun kaynağıdır.

## 6. TransportTask Yaşam Döngüsü

```mermaid
stateDiagram-v2
    [*] --> Pending: "Operator gorev olusturur"
    Pending --> Assigned: "AGV gorev talep eder"
    Assigned --> InProgress: "AGV yuku aldi"
    InProgress --> Completed: "AGV yuku birakti"

    Assigned --> Pending: "AGV reddetti veya zaman asimi"
    InProgress --> Failed: "AGV ariza bildirdi"
    Pending --> Cancelled: "Operator iptal etti"

    Completed --> [*]
    Failed --> [*]
    Cancelled --> [*]

    note right of Assigned
        Gecersiz gecisler aggregate
        icinde reddedilir.
        Ornek: Completed -> Assigned yok.
    end note
```

Durum geçişleri `TransportTask` aggregate'inin içinde uygulanır. Property'ler
`private set`'tir; durum yalnızca metotlarla değişir.

## 7. UC-2 — Görev Talebi ve Concurrency

```mermaid
sequenceDiagram
    participant A1 as AGV-1
    participant A2 as AGV-2
    participant API as Tasks API
    participant H as CommandHandler
    participant R as Repository
    participant DB as PostgreSQL

    A1->>API: POST /tasks/claim
    A2->>API: POST /tasks/claim

    API->>H: ClaimTaskCommand
    H->>R: uygun gorevi yukle
    R->>DB: SELECT (row_version dahil)
    DB-->>R: TransportTask

    Note over H: AssignmentPolicy<br/>hangi AGV uygun

    H->>R: task.Assign(agv1) sonra kaydet
    R->>DB: UPDATE WHERE row_version esit
    DB-->>R: 1 satir guncellendi
    API-->>A1: 200 gorev atandi

    H->>R: ayni gorev icin ikinci kayit
    R->>DB: UPDATE WHERE row_version esit
    DB-->>R: 0 satir - CONCURRENCY CONFLICT
    API-->>A2: 409 gorev artik musait degil
```

**Kabul kriteri:** iki eşzamanlı istek sonrası veritabanında **tek** `TASK_ASSIGNMENT`
satırı bulunur. Bu, Testcontainers ile gerçek PostgreSQL'e karşı test edilir.

## 8. UC-4 — Görev Tamamlama ve Outbox

```mermaid
sequenceDiagram
    participant AGV
    participant T as Tasks modulu
    participant DB as PostgreSQL
    participant W as OutboxProcessor
    participant S as Stock modulu

    AGV->>T: POST /tasks/{id}/complete
    rect rgb(230, 245, 233)
        Note over T,DB: Tek transaction
        T->>DB: TransportTask durumunu Completed yap
        T->>DB: OutboxMessage ekle - TaskCompleted
    end
    T-->>AGV: 200 OK

    W->>DB: islenmemis outbox kayitlarini oku
    W->>S: TaskCompleted olayini ilet
    S->>DB: StockMovement ekle
    W->>DB: outbox kaydini islendi isaretle
```

Yeşil kutu meselenin özü: durum değişikliği ile "bunu duyur" niyeti **aynı transaction**'da
yazılır. Uygulama arada çökerse outbox kaydı durur ve işlem tekrar denenir.

## 9. Kaynak Kilidi

```mermaid
sequenceDiagram
    participant A1 as AGV-1
    participant A2 as AGV-2
    participant API as Tasks API
    participant BG as LockExpiryService

    A1->>API: POST /resources/{id}/lock
    API-->>A1: 200 kilit alindi - expires_at

    A2->>API: POST /resources/{id}/lock
    API-->>A2: 409 kaynak mesgul

    alt Normal akis
        A1->>API: DELETE /resources/{id}/lock
        API-->>A1: 200 birakildi
    else AGV ariza yapti
        BG->>API: suresi dolan kilitleri serbest birak
        Note over BG: IHostedService<br/>periyodik calisir
    end
```

Zaman aşımı olmasaydı arızalanan bir AGV koridoru kalıcı olarak bloke ederdi.
`IHostedService` bu senaryo için var — yapay bir gereksinim değil.

## 10. Fonksiyonel Gereksinimler

| # | Gereksinim | Modül | Use case |
|---|---|---|---|
| FR-01 | AGV kaydedilebilmeli, durumu güncellenebilmeli | Fleet | UC-6 |
| FR-02 | AGV batarya seviyesi bildirebilmeli | Fleet | UC-6 |
| FR-03 | Görev oluşturulabilmeli (kaynak, hedef, malzeme, öncelik) | Tasks | UC-1 |
| FR-04 | AGV bekleyen görev talep edebilmeli | Tasks | UC-2 |
| FR-05 | Bir görev aynı anda tek AGV'ye atanmalı | Tasks | UC-2 |
| FR-06 | Atama politikası değiştirilebilir olmalı | Tasks | UC-2 |
| FR-07 | AGV kaynak kilidi alabilmeli | Tasks | UC-3 |
| FR-08 | Bir kaynağı aynı anda tek AGV tutmalı | Tasks | UC-3 |
| FR-09 | Süresi dolan kilit otomatik serbest kalmalı | Tasks | UC-3 |
| FR-10 | Görev tamamlanabilmeli, geçersiz geçiş reddedilmeli | Tasks | UC-4 |
| FR-11 | Tamamlanan görev stok hareketi üretmeli | Stock | UC-4 |
| FR-12 | Filo durumu listelenebilmeli | Fleet | UC-5 |
| FR-13 | Filo durumu canlı güncellenmeli | Fleet | UC-5 |
| FR-14 | Görev geçmişi sayfalanabilir listelenmeli | Tasks | UC-7 |
| FR-15 | Operator ve Supervisor rolleri ayrılmalı | — | tümü |

## 11. Fonksiyonel Olmayan Gereksinimler

| # | Gereksinim | Ölçüt |
|---|---|---|
| NFR-01 | Görev talebi hızlı yanıtlamalı | < 200 ms |
| NFR-02 | Concurrency garantisi kanıtlanabilir olmalı | Gerçek DB'ye karşı eşzamanlı test |
| NFR-03 | Modül sınırı derleyici ile korunmalı | Modüller birbirinin projesine referans vermez |
| NFR-04 | Kurulum tek komutla yapılmalı | `docker compose up` |
| NFR-05 | Birim testler veritabanı gerektirmemeli | Domain testleri saf C# |
| NFR-06 | Migration dağıtımdan ayrı olmalı | Uygulama açılışında çalışmaz |
| NFR-07 | Sırlar kodda bulunmamalı | Ortam değişkeni |

## 12. Pattern Envanteri

| Pattern | Nerede | Çözdüğü problem |
|---|---|---|
| **Modular Monolith** | Çözüm yapısı | Stok mantığı ile filo mantığı karışmasın |
| **CQRS** | Her modül | Yazma aggregate'ten, okuma projeksiyondan |
| **Aggregate + rich domain** | `TransportTask` | Geçersiz durum geçişi imkânsız olsun |
| **Repository** | `ITransportTaskRepository` | Aggregate bir bütün olarak yüklensin/kaydedilsin |
| **Optimistic Concurrency** | `row_version` | Görev/kilit iki kez verilmesin |
| **State Machine** | Task yaşam döngüsü | Geçişler tek yerde tanımlı olsun |
| **Strategy** | `IAssignmentPolicy` | Atama kuralı değişebilir olsun |
| **Decorator** | `ICommandHandler` sarmalayıcıları | Log ve transaction handler'ı kirletmesin |
| **Outbox** | `Tasks` modülü | Kayıt gitti ama olay gitmedi durumu olmasın |
| **Integration Events** | Modüller arası | Modüller birbirini doğrudan çağırmasın |
| **Hosted Service** | `LockExpiryService` | Takılı kilitler serbest kalsın |
| **Options** | Kilit süresi, batarya eşiği | Ayarlar koda gömülmesin |

## 13. Ekranlar

```mermaid
flowchart LR
    D["<b>Fleet Dashboard</b><br/>AGV durumlari, canli"]
    TL["<b>Task List</b><br/>filtre + sayfalama"]
    TD["<b>Task Detail</b><br/>atama gecmisi, kilitler"]
    NT["<b>New Task</b><br/>gorev olusturma"]

    D -->|"AGV sec"| TD
    TL -->|"gorev sec"| TD
    TL --> NT
    NT -->|"olustur"| TL

    style D fill:#d5e8d4,stroke:#82b366
```

## 14. Kabul Kriterleri

| # | Kriter |
|---|---|
| AC-1 | İki eşzamanlı görev talebinde biri 200, diğeri 409 alır; DB'de tek atama olur |
| AC-2 | Meşgul bir kaynağa ikinci kilit isteği 409 alır |
| AC-3 | Süresi dolan kilit, hosted service çalıştıktan sonra serbest olur |
| AC-4 | `Completed` bir görev tekrar `Assigned` yapılamaz — domain hatası döner |
| AC-5 | Görev tamamlandığında `STOCK_MOVEMENT` satırı oluşur |
| AC-6 | Outbox kaydı işlendikten sonra `processed_at` dolar |
| AC-7 | Operator rolü AGV kaydedemez — 403 alır |
| AC-8 | `docker compose up` sonrası örnek filo ve görevler arayüzde görünür |
