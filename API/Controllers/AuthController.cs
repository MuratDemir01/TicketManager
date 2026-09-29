using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketManager.Services;

namespace TicketManager.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        public class LoginRequest
        {
            [Required]
            public string UserNameOrEmail { get; set; } = null!;

            [Required]
            [MinLength(6)]
            public string Password { get; set; } = null!;
        }

        public class LoginResponse
        {
            public string Token { get; set; } = null!;
            public string UserId { get; set; } = null!;
            public string UserName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string Role { get; set; } = null!;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _auth.LoginAsync(request.UserNameOrEmail, request.Password);
            if (result == null)
                return Unauthorized(new { message = "Kullanıcı adı/e-posta veya şifre hatalı." });

            var (user, token) = result.Value;
            return Ok(new LoginResponse
            {
                Token = token,
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role.ToString()
            });
        }
    }
}
