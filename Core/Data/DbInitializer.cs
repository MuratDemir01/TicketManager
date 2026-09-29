using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketManager.Data;
using TicketManager.Entities;
using TicketManager.Enums;

namespace TicketManager.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            var hasher = new PasswordHasher<User>();

            var seeds = new (string Id, string UserName, string Email, UserRole Role, string Password)[]
            {
                ("admin-ahmet", "ahmet.yilmaz", "ahmet.yilmaz@firma.com", UserRole.Admin, "Admin123!"),
                ("admin-elif", "elif.kaya", "elif.kaya@firma.com", UserRole.Admin, "Admin123!"),
                ("emp-mehmet", "mehmet.demir", "mehmet.demir@firma.com", UserRole.Employee, "Emp123!"),
                ("emp-ayse", "ayse.celik", "ayse.celik@firma.com", UserRole.Employee, "Emp123!"),
                ("emp-can", "can.ozturk", "can.ozturk@firma.com", UserRole.Employee, "Emp123!"),
                ("emp-zeynep", "zeynep.arslan", "zeynep.arslan@firma.com", UserRole.Employee, "Emp123!"),
                ("emp-burak", "burak.sahin", "burak.sahin@firma.com", UserRole.Employee, "Emp123!")
            };

            foreach (var s in seeds)
            {
                if (await db.Users.AnyAsync(u => u.Email == s.Email))
                    continue;

                var user = new User
                {
                    Id = s.Id,
                    UserName = s.UserName,
                    Email = s.Email,
                    Role = s.Role
                };
                user.PasswordHash = hasher.HashPassword(user, s.Password);
                db.Users.Add(user);
            }

            await db.SaveChangesAsync();

            if (await db.Tickets.AnyAsync())
                return;

            var admin = await db.Users.FirstAsync(u => u.Id == "admin-ahmet");
            var mehmet = await db.Users.FirstAsync(u => u.Id == "emp-mehmet");
            var ayse = await db.Users.FirstAsync(u => u.Id == "emp-ayse");
            var now = DateTime.UtcNow;

            db.Tickets.AddRange(
                new Ticket
                {
                    TicketNumber = $"REQ-{now.Year}-00001",
                    Title = "Giriş ekranı yavaş",
                    Description = "Login 5-6 sn sürüyor, müşteri şikayet etti.",
                    CustomerName = "Fatma Aydın",
                    CustomerEmail = "fatma.aydin@ornek.com",
                    Priority = TicketPriority.High,
                    Status = TicketStatus.Assigned,
                    AssignedUserId = mehmet.Id,
                    CreatedByUserId = admin.Id,
                    CreatedAt = now.AddDays(-2)
                },
                new Ticket
                {
                    TicketNumber = $"REQ-{now.Year}-00002",
                    Title = "Fatura PDF inmiyor",
                    Description = "İndir butonu boş dosya veriyor.",
                    CustomerName = "Hasan Koç",
                    CustomerEmail = "hasan.koc@ornek.com",
                    Priority = TicketPriority.Critical,
                    Status = TicketStatus.InProgress,
                    AssignedUserId = ayse.Id,
                    CreatedByUserId = admin.Id,
                    CreatedAt = now.AddDays(-1)
                },
                new Ticket
                {
                    TicketNumber = $"REQ-{now.Year}-00003",
                    Title = "Raporlara tarih filtresi",
                    Description = "Satış raporuna başlangıç-bitiş tarihi isteniyor.",
                    CustomerName = "Selin Aksoy",
                    CustomerEmail = "selin.aksoy@ornek.com",
                    Priority = TicketPriority.Normal,
                    Status = TicketStatus.New,
                    CreatedByUserId = admin.Id,
                    CreatedAt = now
                });

            await db.SaveChangesAsync();
        }
    }
}
