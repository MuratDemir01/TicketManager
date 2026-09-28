using TicketManager.Entities;
using TicketManager.Enums;

namespace TicketManager.Services
{
    // İsterdeki durum geçişlerini TicketService içine if zinciri olarak yazmak istemedim;
    // Sonraki statü ne olmalı kurallarını burası belirliyor.
    public static class TicketStateMachine
    {
        private static readonly Dictionary<TicketStatus, TicketStatus[]> Allowed = new()
        {
            [TicketStatus.New] = new[] { TicketStatus.Assigned },
            [TicketStatus.Assigned] = new[] { TicketStatus.InProgress },
            [TicketStatus.InProgress] = new[] { TicketStatus.OnHold, TicketStatus.Resolved },
            [TicketStatus.OnHold] = new[] { TicketStatus.Closed },
            [TicketStatus.Resolved] = new[] { TicketStatus.Closed },
            [TicketStatus.Closed] = Array.Empty<TicketStatus>()
        };

        public static void EnsureCanModify(Ticket ticket)
        {
            if (ticket.Status == TicketStatus.Closed)
                throw new InvalidOperationException("Closed talep değiştirilemez.");
        }

        public static void EnsureTransition(TicketStatus from, TicketStatus to)
        {
            if (from == TicketStatus.Closed)
                throw new InvalidOperationException("Closed talep başka bir duruma geçirilemez.");

            if (!Allowed.TryGetValue(from, out var nextStates) || !nextStates.Contains(to))
                throw new InvalidOperationException($"Geçersiz durum geçişi: {from} -> {to}");
        }
    }
}
