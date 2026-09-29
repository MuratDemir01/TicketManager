using System.Net;
using System.Text.Json;

namespace TicketManager.API.Middleware
{
    // Her controller metoduna exception yazmak yerine hataları ortak bir yerden yönetmek istedim.
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await WriteError(context, ex);
            }
        }

        private async Task WriteError(HttpContext context, Exception ex)
        {
            var (status, message) = ex switch
            {
                KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
                InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Forbidden, ex.Message),
                ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "Beklenmeyen bir hata oluştu.")
            };

            if (status == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "İşlenmeyen hata");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;

            var body = JsonSerializer.Serialize(new
            {
                status = (int)status,
                message
            });

            await context.Response.WriteAsync(body);
        }
    }
}
