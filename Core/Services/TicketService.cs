using Microsoft.EntityFrameworkCore;
using TicketManager.Data;
using TicketManager.Entities;
using TicketManager.Enums;

namespace TicketManager.Services
{
    public class TicketService : ITicketService
    {
        private readonly AppDbContext _db;
        private readonly ITicketNumberGenerator _numbers;

        public TicketService(AppDbContext db, ITicketNumberGenerator numbers)
        {
            _db = db;
            _numbers = numbers;
        }

        public async Task<(IReadOnlyList<Ticket> Items, int TotalCount, int Page, int PageSize)> GetListAsync(
            string? search,
            TicketStatus? status,
            TicketPriority? priority,
            string? assignedUserId,
            int page = 1,
            int pageSize = 20)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            IQueryable<Ticket> tickets = _db.Tickets.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                tickets = tickets.Where(x =>
                    x.TicketNumber.Contains(term) ||
                    x.Title.Contains(term));
            }

            if (status.HasValue)
                tickets = tickets.Where(x => x.Status == status.Value);

            if (priority.HasValue)
                tickets = tickets.Where(x => x.Priority == priority.Value);

            if (!string.IsNullOrWhiteSpace(assignedUserId))
                tickets = tickets.Where(x => x.AssignedUserId == assignedUserId);

            var totalCount = await tickets.CountAsync();
            var items = await tickets
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount, page, pageSize);
        }

        public async Task<Ticket?> GetByIdAsync(int id)
        {
            return await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Ticket> CreateAsync(
            string title,
            string description,
            string customerName,
            string customerEmail,
            TicketPriority priority)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var ticket = new Ticket
                {
                    TicketNumber = await _numbers.GenerateAsync(),
                    Title = title.Trim(),
                    Description = description.Trim(),
                    CustomerName = customerName.Trim(),
                    CustomerEmail = customerEmail.Trim(),
                    Priority = priority,
                    Status = TicketStatus.New,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Tickets.Add(ticket);

                try
                {
                    // Önce ticket, sonra history ekliyoruz ki ticket id'si history'de de kullanılabilir olsun. 
                    await _db.SaveChangesAsync();
                    AddHistory(ticket.Id, "Created", "Ticket", null, ticket.TicketNumber);
                    await _db.SaveChangesAsync();
                    return ticket;
                }
                // Unique çakışırsa numarayı yeniden üretip tekrar deniyoruz.
                catch (DbUpdateException) when (attempt < 2)
                {
                    // Transaction tamamlanmaz, bir noktada hata alırsa tüm db süreci geri alınıyor.
                    _db.ChangeTracker.Clear();
                }
            }

            throw new InvalidOperationException("Talep numarası üretilemedi.");
        }

        public async Task<Ticket> UpdateAsync(
            int id,
            string title,
            string description,
            string customerName,
            string customerEmail,
            TicketPriority priority)
        {
            var ticket = await GetTrackedTicket(id);
            TicketStateMachine.EnsureCanModify(ticket);

            ticket.Title = title.Trim();
            ticket.Description = description.Trim();
            ticket.CustomerName = customerName.Trim();
            ticket.CustomerEmail = customerEmail.Trim();
            ticket.UpdatedAt = DateTime.UtcNow;

            if (ticket.Priority != priority)
            {
                var oldPriority = ticket.Priority;
                ticket.Priority = priority;
                AddHistory(ticket.Id, "PriorityChanged", "Priority", oldPriority.ToString(), priority.ToString());
            }

            await _db.SaveChangesAsync();
            return ticket;
        }

        public async Task DeleteAsync(int id)
        {
            var ticket = await GetTrackedTicket(id);
            TicketStateMachine.EnsureCanModify(ticket);
            _db.Tickets.Remove(ticket);
            await _db.SaveChangesAsync();
        }

        public async Task<Ticket> ChangeStatusAsync(int id, TicketStatus newStatus)
        {
            var ticket = await GetTrackedTicket(id);
            TicketStateMachine.EnsureCanModify(ticket);
            TicketStateMachine.EnsureTransition(ticket.Status, newStatus);

            var oldStatus = ticket.Status;
            ticket.Status = newStatus;
            ticket.UpdatedAt = DateTime.UtcNow;
            AddHistory(ticket.Id, "StatusChanged", "Status", oldStatus.ToString(), newStatus.ToString());
            await _db.SaveChangesAsync();
            return ticket;
        }

        public async Task<Ticket> AssignAsync(int id, string? assignedUserId)
        {
            var ticket = await GetTrackedTicket(id);
            TicketStateMachine.EnsureCanModify(ticket);

            var oldAssignee = ticket.AssignedUserId;
            var normalized = string.IsNullOrWhiteSpace(assignedUserId) ? null : assignedUserId.Trim();

            if (oldAssignee != normalized)
            {
                ticket.AssignedUserId = normalized;
                ticket.UpdatedAt = DateTime.UtcNow;
                AddHistory(ticket.Id, "Assigned", "AssignedUserId", oldAssignee, ticket.AssignedUserId);
            }

            if (ticket.Status == TicketStatus.New && !string.IsNullOrWhiteSpace(ticket.AssignedUserId))
            {
                TicketStateMachine.EnsureTransition(ticket.Status, TicketStatus.Assigned);
                var oldStatus = ticket.Status;
                ticket.Status = TicketStatus.Assigned;
                ticket.UpdatedAt = DateTime.UtcNow;
                AddHistory(ticket.Id, "StatusChanged", "Status", oldStatus.ToString(), TicketStatus.Assigned.ToString());
            }

            await _db.SaveChangesAsync();
            return ticket;
        }

        public async Task<TicketNote> AddNoteAsync(int ticketId, string noteText)
        {
            if (string.IsNullOrWhiteSpace(noteText))
                throw new InvalidOperationException("Not boş olamaz.");

            var ticket = await GetTrackedTicket(ticketId);
            TicketStateMachine.EnsureCanModify(ticket);

            var note = new TicketNote
            {
                TicketId = ticketId,
                NoteText = noteText.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.TicketNotes.Add(note);
            ticket.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return note;
        }

        private async Task<Ticket> GetTrackedTicket(int id)
        {
            var ticket = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id);
            if (ticket == null)
                throw new KeyNotFoundException($"Ticket {id} bulunamadı.");
            return ticket;
        }

        private void AddHistory(int ticketId, string action, string fieldName, string? oldValue, string? newValue)
        {
            _db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticketId,
                Action = action,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedAt = DateTime.UtcNow
            });
        }
    }
}
