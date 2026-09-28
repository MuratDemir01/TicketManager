Bu projedeki uygulama kodlarını ve testleri kişisel olarak geliştirdiğimi, kod üreten yapay zekâ araçlarından veya başka bir kişiden geliştirme desteği almadığımı beyan ederim.

# TicketManager

Müşteri talep (ticket) yönetimi için .NET 8 Web API projesi.

## Projenin çalıştırılma adımları

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) kurulu olmalı.
2. 'TicketManager\API' klasörüne gir.
3. Paketleri yükle ve API'yi çalıştır:
   - dotnet restore
   - dotnet run --project API
4. Swagger UI adresleri:
   - http://localhost:5280/swagger
   - https://localhost:7280/swagger
5. Örnek talep oluşturma 'POST /api/tickets':

{
  "title": "Ödeme ekranı donuyor",
  "description": "Kart bilgisi girince sayfa yanıt vermiyor.",
  "customerName": "Ali Veli",
  "customerEmail": "ali.veli@ornek.com",
  "priority": 2
}

## Kullanılan teknolojiler

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core 8
- SQLite
- Swashbuckle (Swagger)

## Test kullanıcıları ve rolleri

Projede kimlik doğrulama yok. Rol de tanımlı değil. Endpointler şu an herkese açık.

## Veritabanının nasıl oluşturulacağı

- Dosya yolu: proje klasöründe 'DB/app.db'
- 'API/Program.cs' çalışırken ContentRoot'un bir üstünde DB klasörünü oluşturur. SQLite bağlantısı da buraya gider.
- Migration dosyaları 'Core/Data/Migrations' altında.
- Uygulama açılınca 'Database.Migrate()' bekleyen migrationları uygular.
- Model değişince yeni migration şöyle eklenir:
   - dotnet ef migrations add IsimVer --project Core --startup-project API --output-dir Data/Migrations
   - dotnet run --project API

## Testlerin nasıl çalıştırılacağı

Otomatik test projesi şimdilik yok.

## Uygulanan mimari yaklaşım

- Solution: 'TicketManager.sln'
  - 'API' ('TicketManager.API'): Web API, controllerlar, request/response DTOlar, 'PagedResult'
  - 'Core' ('TicketManager.Core'): entity'ler, enumlar, DbContext, migrationlar, servisler
  - 'ConsoleApplications': ileride eklenecek mini konsol uygulamaları. Şu an boş.
- 'API' projesi 'Core'a project reference ile bağlı.

## Önemli teknik kararlar

- Şema EF Core migration ile yönetiliyor. 'EnsureCreated' kullanılmıyor. Uygulama açılırken 'Migrate()' çalışıyor.
- Ticket numarası 'REQ-{YEAR}-{#####}' formatında. Üretim 'TicketNumberGenerator' ile yapılıyor. Kolonda unique index var.
- Durum geçişleri 'TicketStateMachine' ile sınırlı.
- Request DTOlar controller içinde.

## Karşılaşılan en az iki teknik problem ve çözümleri

1. **'EnsureCreated' şemayı güncellemiyordu**  
   Entity değişince mevcut 'app.db' eski şemada kalıyordu.  
   **Çözüm:** EF Core migrationa geçtim. Startupta 'Migrate()' çağrılıyor. Model değişince 'dotnet ef migrations add ...' ile yeni migration ekliyorum.

2. **Eşzamanlı talep numarası çakışması riski**  
   Aynı anda iki istek gelince aynı numara üretilebiliyordu.  
   **Çözüm:** 'TicketNumberGenerator' içinde lock ve transaction kullandım. Unique index var. Create tarafında çakışırsa yeniden deniyorum.

## Tamamlanamayan bölümler

- Unit ve integration test yok.
- Auth ve roller yok.

## Bilinen hatalar

- SQLite 'HasMaxLength' uzunluğu runtime'da zorlamıyor. API DTOlarda da DataAnnotations validasyonu yok. Örneğin title 150 karakterden uzun olsa da istek geçebilir.

## Yararlanılan önemli dokümantasyon kaynakları

- https://learn.microsoft.com/tr-tr/ef/core/providers/sqlite/?tabs=dotnet-core-cli — SQLite + EF
- https://learn.microsoft.com/tr-tr/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli — migration üretme
- https://stackoverflow.com/questions/38238043/how-and-where-to-call-database-ensurecreated-and-database-migrate — 'EnsureCreated' yerine 'Migrate' kullanma nedeni
- https://stackoverflow.com/questions/75031802/entity-framework-core-modelbuilder-applyconfigurationsfromassembly-scan-all-conf — 'ApplyConfigurationsFromAssembly' ne tarar
- https://stackoverflow.com/questions/69800474/run-ef-migrations-script-on-separate-projects-net5 — Coreda migration, API startup ('--startup-project')
- https://stackoverflow.com/questions/5923767/simple-state-machine-example-in-c — durum geçişi için dictionary state machine fikri
- https://learn.microsoft.com/tr-tr/dotnet/api/system.threading.semaphoreslim?view=net-10.0 — process içi 'Gate' kilidi
- https://learn.microsoft.com/tr-tr/dotnet/standard/asynchronous-programming-patterns/async-coordination-primitives-advanced — 'SemaphoreSlim(1, 1)' ile 'WaitAsync' ve 'Release' örnek kod
- https://stackoverflow.com/questions/76134865/how-to-release-semaphoreslim — static 'SemaphoreSlim(1, 1)' ve try/finally (Gate ile aynı kalıp)
- https://stackoverflow.com/questions/62577492/what-is-the-correct-usage-of-sempahoreslim-as-a-lock-in-async-code — async lock olarak doğru kullanım
- https://stackoverflow.com/questions/42885020/how-to-discard-changes-to-context-in-ef-core — unique çakışınca veya hata alınca ChangeTracker temizleme 'Clear()'
