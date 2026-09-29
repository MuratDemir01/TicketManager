using Microsoft.EntityFrameworkCore;
using Moq;
using TicketManager.Data;
using TicketManager.Entities;
using TicketManager.Enums;
using TicketManager.Services;

namespace TicketManager.Tests
{
    public class TicketServiceTests
    {
        private static AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static TicketService CreateService(AppDbContext db)
        {
            var numbers = new Mock<ITicketNumberGenerator>();
            numbers
                .Setup(x => x.GenerateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("REQ-2026-00001");

            return new TicketService(db, numbers.Object);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Bos_veya_null_not_reddedilir(string? noteText)
        {
            await using var db = CreateDb();
            db.Tickets.Add(new Ticket
            {
                TicketNumber = "REQ-2026-00001",
                Title = "Test",
                Description = "Desc",
                CustomerName = "Ali",
                CustomerEmail = "ali@ornek.com",
                TicketStatus = TicketStatus.Assigned,
                AssignedUserId = "user-a",
                CreatedByUserId = "admin-user",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.AddNoteAsync(1, noteText!, "user-a", isAdmin: false));
        }

        [Fact]
        public async Task Baska_calisana_atanmis_talep_guncellenemez()
        {
            await using var db = CreateDb();
            db.Tickets.Add(new Ticket
            {
                TicketNumber = "REQ-2026-00002",
                Title = "Atanmış talep",
                Description = "Desc",
                CustomerName = "Ali",
                CustomerEmail = "ali@ornek.com",
                TicketStatus = TicketStatus.Assigned,
                AssignedUserId = "user-a",
                Priority = TicketPriority.Normal,
                CreatedByUserId = "admin-user",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ChangeStatusAsync(1, TicketStatus.InProgress, "user-b", isAdmin: false));

            Assert.Contains("atanmamış", ex.Message);
        }

        [Fact]
        public async Task Admin_rolundeki_kullaniciya_atama_reddedilir()
        {
            await using var db = CreateDb();
            db.Users.Add(new User
            {
                Id = "admin-user",
                UserName = "admin",
                Email = "admin@test.com",
                PasswordHash = "x",
                Role = UserRole.Admin
            });
            db.Tickets.Add(new Ticket
            {
                TicketNumber = "REQ-2026-00003",
                Title = "Atama testi",
                Description = "Desc",
                CustomerName = "Ali",
                CustomerEmail = "ali@ornek.com",
                TicketStatus = TicketStatus.New,
                CreatedByUserId = "admin-user",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AssignAsync(1, "admin-user", "admin-user"));

            Assert.Contains("Employee", ex.Message);
        }

        [Fact]
        public async Task Calisan_yalniz_kendisine_atanmis_talepleri_gorur()
        {
            await using var db = CreateDb();
            db.Tickets.AddRange(
                new Ticket
                {
                    TicketNumber = "REQ-2026-00010",
                    Title = "Benim talebim",
                    Description = "Desc",
                    CustomerName = "Ali",
                    CustomerEmail = "ali@ornek.com",
                    TicketStatus = TicketStatus.Assigned,
                    AssignedUserId = "user-a",
                    CreatedByUserId = "admin-user",
                    CreatedAt = DateTime.UtcNow
                },
                new Ticket
                {
                    TicketNumber = "REQ-2026-00011",
                    Title = "Baskasinin talebi",
                    Description = "Desc",
                    CustomerName = "Veli",
                    CustomerEmail = "veli@ornek.com",
                    TicketStatus = TicketStatus.Assigned,
                    AssignedUserId = "user-b",
                    CreatedByUserId = "admin-user",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-1)
                });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            // Query'den assignedUserId=user-b gelse bile restriction user-a'yı zorlar.
            var (items, totalCount, _, _) = await service.GetListAsync(
                search: null,
                ticketStatus: null,
                priority: null,
                assignedUserId: "user-b",
                page: 1,
                pageSize: 20,
                employeeIdForRestriction: "user-a");

            Assert.Equal(1, totalCount);
            Assert.Single(items);
            Assert.Equal("user-a", items[0].AssignedUserId);
            Assert.Equal("Benim talebim", items[0].Title);
        }

        [Fact]
        public async Task Ayni_talep_numarasi_iki_kez_olusturulamaz()
        {
            // Unique index InMemory'de zorlanmıyor; SQLite in-memory kullanıyoruz.
            await using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();

            db.Tickets.Add(new Ticket
            {
                TicketNumber = "REQ-2026-00001",
                Title = "Ilk",
                Description = "Desc",
                CustomerName = "Ali",
                CustomerEmail = "ali@ornek.com",
                TicketStatus = TicketStatus.New,
                CreatedByUserId = "admin-user",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            // Generator her denemede aynı numarayı verirse create 3 denemeden sonra düşmeli.
            var numbers = new Mock<ITicketNumberGenerator>();
            numbers
                .Setup(x => x.GenerateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("REQ-2026-00001");

            var service = new TicketService(db, numbers.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateAsync(
                    "Ikinci",
                    "Desc",
                    "Ali",
                    "ali@ornek.com",
                    TicketPriority.Normal,
                    "admin-user",
                    assignedUserId: null));

            Assert.Contains("Talep numarası üretilemedi", ex.Message);
            Assert.Equal(1, await db.Tickets.CountAsync());
        }
    }
}
