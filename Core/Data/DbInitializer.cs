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
                    Role = s.Role,
                    Status = Status.Active
                };
                user.PasswordHash = hasher.HashPassword(user, s.Password);
                db.Users.Add(user);
            }

            await db.SaveChangesAsync();

            if (!await db.Tickets.AnyAsync())
            {
                var admin = await db.Users.FirstAsync(u => u.Id == "admin-ahmet");
                var mehmet = await db.Users.FirstAsync(u => u.Id == "emp-mehmet");
                var ayse = await db.Users.FirstAsync(u => u.Id == "emp-ayse");
                var now = DateTime.UtcNow;

                var seedTickets = new[]
                {
                    new Ticket
                    {
                        TicketNumber = $"REQ-{now.Year}-00001",
                        Title = "Giriş ekranı yavaş",
                        Description = "Login 5-6 sn sürüyor, müşteri şikayet etti.",
                        CustomerName = "Fatma Aydın",
                        CustomerEmail = "fatma.aydin@ornek.com",
                        Priority = TicketPriority.High,
                        TicketStatus = TicketStatus.Assigned,
                        Status = Status.Active,
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
                        TicketStatus = TicketStatus.InProgress,
                        Status = Status.Active,
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
                        TicketStatus = TicketStatus.New,
                        Status = Status.Active,
                        CreatedByUserId = admin.Id,
                        CreatedAt = now
                    }
                };

                db.Tickets.AddRange(seedTickets);
                await db.SaveChangesAsync();

                // SaveChanges sonrası EF Id'leri doldurur, TicketId history için gerekli.
                foreach (var t in seedTickets)
                {
                    db.TicketHistories.Add(new TicketHistory
                    {
                        TicketId = t.Id,
                        Action = "Created",
                        FieldName = "Ticket",
                        NewValue = t.TicketNumber,
                        ChangedByUserId = t.CreatedByUserId ?? admin.Id,
                        ChangedAt = t.CreatedAt,
                        Status = Status.Active
                    });
                    if (!string.IsNullOrWhiteSpace(t.AssignedUserId))
                    {
                        db.TicketHistories.Add(new TicketHistory
                        {
                            TicketId = t.Id,
                            Action = "Assigned",
                            FieldName = "AssignedUserId",
                            NewValue = t.AssignedUserId,
                            ChangedByUserId = t.CreatedByUserId ?? admin.Id,
                            ChangedAt = t.CreatedAt.AddSeconds(1),
                            Status = Status.Active
                        });
                    }
                }

                await db.SaveChangesAsync();
            }
        }
    }
}
