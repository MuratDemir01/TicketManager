namespace TicketManager.Services
{
    public interface ITicketNumberGenerator
    {
        Task<string> GenerateAsync(CancellationToken cancellationToken = default);
    }
}
