using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketManager.Auth;
using TicketManager.Enums;
using TicketManager.Services;

namespace TicketManager.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = AppRoles.Admin)]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _users;

        public UsersController(IUserService users)
        {
            _users = users;
        }

        public class UserDto
        {
            public string Id { get; set; } = null!;
            public string UserName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string Role { get; set; } = null!;
        }

        public class CreateUserDto
        {
            [Required, MaxLength(100)]
            public string UserName { get; set; } = null!;

            [Required, EmailAddress, MaxLength(256)]
            public string Email { get; set; } = null!;

            [Required, MinLength(6), MaxLength(100)]
            public string Password { get; set; } = null!;

            [Required]
            public UserRole Role { get; set; }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var list = await _users.GetAllAsync();
            return Ok(list.Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                Role = u.Role.ToString()
            }));
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create(CreateUserDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!Enum.IsDefined(typeof(UserRole), dto.Role))
            {
                ModelState.AddModelError(nameof(dto.Role), "Rol Admin veya Employee olmalı.");
                return ValidationProblem(ModelState);
            }

            var user = await _users.CreateAsync(dto.UserName, dto.Email, dto.Password, dto.Role);
            return CreatedAtAction(nameof(GetAll), new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role.ToString()
            });
        }
    }
}
