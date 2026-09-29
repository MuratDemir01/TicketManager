using System.Data;
using Microsoft.EntityFrameworkCore;
using TicketManager.Data;

namespace TicketManager.Services
{
    public class TicketNumberGenerator : ITicketNumberGenerator
    {
        // Aynı anda iki istek gelince aynı numarayı üretmesin diye. Unique index de var ama yine de burayı kilitliyorum.
        private static readonly SemaphoreSlim Gate = new(1, 1);

        private readonly AppDbContext _db;

        public TicketNumberGenerator(AppDbContext db)
        {
            _db = db;
        }

        public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
        {
            await Gate.WaitAsync(cancellationToken);
            try
            {
                // İki istek aynı anda son numarayı görüp aynı REQ'yi üretmesin diye.
                await using var tx = await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

                var year = DateTime.UtcNow.Year;
                var prefix = $"REQ-{year}-";

                // Soft-delete filtreli satırlar unique index'te durur; numarayı onların da üstünden üret.
                var lastNumber = await _db.Tickets
                    .IgnoreQueryFilters()
                    .Where(x => x.TicketNumber.StartsWith(prefix))
                    .OrderByDescending(x => x.TicketNumber)
                    .Select(x => x.TicketNumber)
                    .FirstOrDefaultAsync(cancellationToken);

                var next = 1;
                if (lastNumber != null)
                {
                    var seq = lastNumber.Substring(prefix.Length);
                    if (int.TryParse(seq, out var current))
                        next = current + 1;
                }

                var ticketNumber = $"{prefix}{next:D5}";
                await tx.CommitAsync(cancellationToken);
                return ticketNumber;
            }
            finally
            {
                Gate.Release();
            }
        }
    }
}
