using Microsoft.EntityFrameworkCore;
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Veri tabanı migration ile güncelleniyor.
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
