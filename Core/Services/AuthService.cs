using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TicketManager.Data;
using TicketManager.Entities;

namespace TicketManager.Services
{
    public interface IAuthService
    {
        Task<(User User, string Token)?> LoginAsync(string userNameOrEmail, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly PasswordHasher<User> _hasher = new();

        public AuthService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<(User User, string Token)?> LoginAsync(string userNameOrEmail, string password)
        {
            var key = userNameOrEmail.Trim();
            var user = await _db.Users.FirstOrDefaultAsync(x =>
                x.Email == key || x.UserName == key);

            if (user == null)
                return null;

            var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verify == PasswordVerificationResult.Failed)
                return null;

            return (user, CreateToken(user));
        }

        private string CreateToken(User user)
        {
            var jwt = _config.GetSection("Jwt");
            var keyText = Environment.GetEnvironmentVariable("JWT_KEY")
                ?? throw new InvalidOperationException("JWT_KEY ortam değişkeni bulunamadı.");
            if (keyText.Length < 32)
                throw new InvalidOperationException("JWT_KEY en az 32 karakter olmalı.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresMinutes = int.TryParse(jwt["ExpiresMinutes"], out var m) ? m : 480;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
