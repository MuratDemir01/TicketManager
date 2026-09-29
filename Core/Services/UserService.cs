using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketManager.Data;
using TicketManager.Entities;
using TicketManager.Enums;

namespace TicketManager.Services
{
    public interface IUserService
    {
        Task<IReadOnlyList<User>> GetAllAsync();
        Task<User> CreateAsync(string userName, string email, string password, UserRole role);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<User> _hasher = new();

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<User>> GetAllAsync()
        {
            return await _db.Users
                .AsNoTracking()
                .OrderBy(x => x.UserName)
                .ToListAsync();
        }

        public async Task<User> CreateAsync(string userName, string email, string password, UserRole role)
        {
            if (!Enum.IsDefined(typeof(UserRole), role))
                throw new InvalidOperationException("Rol Admin veya Employee olmalı.");

            var name = userName.Trim();
            var mail = email.Trim();

            var exists = await _db.Users.AnyAsync(u =>
                u.UserName == name || u.Email == mail);
            if (exists)
                throw new InvalidOperationException("Bu kullanıcı adı veya e-posta zaten kayıtlı.");

            var user = new User
            {
                UserName = name,
                Email = mail,
                Role = role
            };
            user.PasswordHash = _hasher.HashPassword(user, password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }
    }
}
