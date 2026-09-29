using System.Security.Claims;
using TicketManager.Auth;

namespace TicketManager.API.Controllers
{
    // Ticket/Users controller'larında JWT claim okumak için ortak yardımcı.
    internal static class CurrentUser
    {
        public static string Id(ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı.");

        public static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole(AppRoles.Admin);
    }
}
