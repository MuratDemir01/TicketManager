Bu projedeki uygulama kodlarını ve testleri kişisel olarak geliştirdiğimi, kod üreten yapay zekâ araçlarından veya başka bir kişiden geliştirme desteği almadığımı beyan ederim.

# TicketManager

Müşteri talep (ticket) yönetimi için .NET 8 Web API projesi.

## Projenin çalıştırılma adımları

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) kurulu olmalı.
2. 'TicketManager\API' klasörüne gir.
3. JWT imza anahtarını makine ortam değişkeni (user değil) olarak ayarla (en az 32 karakter olmalıdır ve yönetici yetkisi gerekir). Ayar sonrası Visual Studio / terminali yeniden aç:
   - PowerShell (Yönetici): `[Environment]::SetEnvironmentVariable("JWT_KEY", "en-az-32-karakter-bir-anahtar-buraya", "Machine")`
   - cmd (Yönetici): `setx JWT_KEY "en-az-32-karakter-bir-anahtar-buraya" /M`
4. Paketleri yükle ve API'yi çalıştır:
   - dotnet restore
   - dotnet run --project API
5. Swagger UI adresleri:
   - http://localhost:5280/swagger
   - https://localhost:7280/swagger
6. Önce login al ('POST /api/auth/login'):

{
  "userNameOrEmail": "ahmet.yilmaz@firma.com",
  "password": "Admin123!"
}

Swagger'da Authorize → 'Bearer {token}' yapıştır. Sonra ticket endpointlerini kullan.

## Kullanılan teknolojiler

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core 8
- SQLite
- JWT Bearer + Identity PasswordHasher
- Swashbuckle (Swagger)

## Test kullanıcıları ve rolleri

| Rol | Kullanıcı | E-posta | Şifre |
|-----|-----------|---------|-------|
| Admin | ahmet.yilmaz | ahmet.yilmaz@firma.com | Admin123! |
| Admin | elif.kaya | elif.kaya@firma.com | Admin123! |
| Employee | mehmet.demir | mehmet.demir@firma.com | Emp123! |
| Employee | ayse.celik | ayse.celik@firma.com | Emp123! |
| Employee | can.ozturk | can.ozturk@firma.com | Emp123! |
| Employee | zeynep.arslan | zeynep.arslan@firma.com | Emp123! |
| Employee | burak.sahin | burak.sahin@firma.com | Emp123! |

Admin: Tüm talepler, oluşturma, atama, öncelik, not, durum, kullanıcı listeleme ve ekleme.  
Employee: Yalnız kendisine atanmış talepler, kendi talebinde not ve durum.

## Veritabanının nasıl oluşturulacağı

- Dosya yolu: proje klasöründe 'DB/app.db'
- 'API/Program.cs' çalışırken ContentRoot'un bir üstünde DB klasörünü oluşturur. SQLite bağlantısı da buraya gider.
- Migration dosyaları 'Core/Data/Migrations' altında.
- Uygulama açılınca 'Database.Migrate()' bekleyen migrationları uygular.
- Model değişince yeni migration şöyle eklenir:
   - dotnet ef migrations add IsimVer --project Core --startup-project API --output-dir Data/Migrations
   - dotnet run --project API

## Testlerin nasıl çalıştırılacağı

   - dotnet test Tests/TicketManager.Tests.csproj

## Uygulanan mimari yaklaşım

- Solution: 'TicketManager.sln'
  - 'API' ('TicketManager.API'): Web API, controllerlar, request/response DTOlar, 'PagedResult'
  - 'Core' ('TicketManager.Core'): entity'ler, enumlar, DbContext, migrationlar, servisler
  - 'Tests' ('TicketManager.Tests'): xUnit unit testleri
  - 'ConsoleApplications': ileride eklenecek mini konsol uygulamaları. Şu an boş.
- 'API' projesi 'Core'a project reference ile bağlı.

## Önemli teknik kararlar

- Şema EF Core migration ile yönetiliyor. 'EnsureCreated' kullanılmıyor. Uygulama açılırken 'Migrate()' çalışıyor.
- Ticket numarası 'REQ-{YEAR}-{#####}' formatında. Üretim 'TicketNumberGenerator' ile yapılıyor. Kolonda unique index var.
- Durum geçişleri 'TicketStateMachine' ile sınırlı.
- JWT ile Admin / Employee yetkisi. Employee listesi 'AssignedUserId == currentUserId' ile kısıtlı.
- Request DTOlar controller içinde
- DataAnnotations ile temel validasyon var.

## Karşılaşılan en az iki teknik problem ve çözümleri

1. **'EnsureCreated' şemayı güncellemiyordu**  
   Entity değişince mevcut 'app.db' eski şemada kalıyordu.  
   **Çözüm:** EF Core migrationa geçtim. Startupta 'Migrate()' çağrılıyor. Model değişince 'dotnet ef migrations add ...' ile yeni migration ekliyorum.

2. **Eşzamanlı talep numarası çakışması riski**  
   Aynı anda iki istek gelince aynı numara üretilebiliyordu.  
   **Çözüm:** 'TicketNumberGenerator' içinde lock ve transaction kullandım. Unique index var. Create tarafında çakışırsa yeniden deniyorum.

## Tamamlanamayan bölümler

- UI henüz yok.

## Bilinen hatalar

- SQLite 'HasMaxLength' kolon uzunluğunu runtimeda zorlamıyordu, API tarafında DataAnnotations ile kısıtladık.
- JWT imza anahtarı 'appsettings.json'da yok; makine ortam değişkeni (user değil) 'JWT_KEY' eksik/kısa ise veya Visual Studio yeniden açılmadıysa API açılmaz.

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
- https://learn.microsoft.com/tr-tr/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-8.0 — JWT Bearer kimlik doğrulamasını Program.cs'te kurma
- https://stackoverflow.com/questions/55137980/create-jwt-token-in-c-sharp-asp-net-core-web-api — login sonrası JwtSecurityToken üretme
- https://stackoverflow.com/questions/4181198/how-to-hash-a-password — PasswordHasher ile şifre hashleme ve doğrulama
- https://stackoverflow.com/questions/76085334/problem-with-jwt-authorization-in-asp-net-core — JWT'de rol claiminin Authorize ile nasıl eşleştiği
- https://learn.microsoft.com/tr-tr/aspnet/core/fundamentals/middleware/write?view=aspnetcore-8.0 — GlobalExceptionMiddleware tarzı özel middleware yazma
- https://stackoverflow.com/questions/70605781/how-to-have-the-same-response-format-for-400-response-raised-from-badrequest-and — ModelState hatalarını ValidationProblem ile döndürme
- https://stackoverflow.com/questions/59027413/how-do-you-protect-your-jwt-symmetric-security-key — JWT_KEY'i ortam değişkeninden okuma
- https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions — User.Role enumunu veritabanında string tutma (HasConversion)
