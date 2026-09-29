using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TicketManager.API.Middleware;
using TicketManager.Data;
using TicketManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Başlaması kolay olduğu için SQLite tercih ettim.
// DB'yi API bin'ine gömmek istemedim; 
// Hem API hem de ileride Reporter vb. console uygulamaları aynı dosyayı görsün diye DB'yi üst klasörde tutuyorum.
var dbFolder = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "DB"));
Directory.CreateDirectory(dbFolder);
var dbPath = Path.Combine(dbFolder, "app.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<ITicketNumberGenerator, TicketNumberGenerator>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

var jwt = builder.Configuration.GetSection("Jwt");
// Secret key sadece env'den alınır, kaynak kodda veya appsettings'te bulunmaz. ReadMe'de açıklandı.
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT_KEY ortam değişkeni eksik veya kısa (en az 32 karakter).");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Dashboard", policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Enum'ı ToString yapmak gerekmesin veya string'e ToEnum metodu yazmamız gerekmesin diye 
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TicketManager API",
        Version = "v1",
        Description =
            "Müşteri talep yönetim API'si. Önce login olmanız gerekir.\n\n" +
            "Roller: Admin (tüm talepler, kullanıcılar), Employee (yalnız atandığı talepler)."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "JWT Bearer. Authorize kutusuna yalnızca token yapıştırın (\"Bearer: \" otomatik eklenir).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Veri tabanı migration ile güncelleniyor.
    ApplyMigrationsSafely(db);
    await DbInitializer.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.DocumentTitle = "TicketManager API";
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "TicketManager v1");
        o.DisplayRequestDuration();
        o.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    });
}

app.UseHttpsRedirection();
app.UseCors("Dashboard");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static void ApplyMigrationsSafely(AppDbContext db)
{
    try
    {
        db.Database.Migrate();
    }
    catch (SqliteException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
    {
        db.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """);

        foreach (var migrationId in db.Database.GetMigrations())
        {
            db.Database.ExecuteSqlRaw(
                """
                INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ({0}, {1});
                """,
                migrationId,
                "8.0.11");
        }
    }
}
