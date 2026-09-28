using TicketManager.Entities;
using TicketManager.Enums;

namespace TicketManager.Services
{
    public interface ITicketService
    {
        Task<(IReadOnlyList<Ticket> Items, int TotalCount, int Page, int PageSize)> GetListAsync(
            string? search,
            TicketStatus? status,
            TicketPriority? priority,
            string? assignedUserId,
            int page = 1,
            int pageSize = 20);
        Task<Ticket?> GetByIdAsync(int id);
        Task<Ticket> CreateAsync(
            string title,
            string description,
            string customerName,
            string customerEmail,
            TicketPriority priority);
        Task<Ticket> UpdateAsync(
            int id,
            string title,
            string description,
            string customerName,
            string customerEmail,
            TicketPriority priority);
        Task DeleteAsync(int id);
        Task<Ticket> ChangeStatusAsync(int id, TicketStatus newStatus);
        Task<Ticket> AssignAsync(int id, string? assignedUserId);
        Task<TicketNote> AddNoteAsync(int ticketId, string noteText);
    }
}
