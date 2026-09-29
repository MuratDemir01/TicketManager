using Microsoft.EntityFrameworkCore;
using TicketManager.Data;
using TicketManager.Dtos;
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
            int page,
            int pageSize,
            string? employeeIdForRestriction);

        Task<Ticket?> GetByIdAsync(int id, string? employeeIdForRestriction);

        Task<Ticket> CreateAsync(
            string title,
            string description,
            string customerName,
            string customerEmail,
            TicketPriority priority,
            string createdByUserId,
            string? assignedUserId);

        Task<Ticket> UpdateAsync(
            int id,
            string title,
            string description,
            string customerName,
            string customerEmail);

        Task DeleteAsync(int id);

        Task<Ticket> ChangeStatusAsync(int id, TicketStatus newStatus, string actingUserId, bool isAdmin);

        Task<Ticket> ChangePriorityAsync(int id, TicketPriority priority, string actingUserId);

        Task<Ticket> AssignAsync(int id, string? assignedUserId, string actingUserId);

        Task<TicketNote> AddNoteAsync(int ticketId, string noteText, string actingUserId, bool isAdmin);

        Task<TicketSummaryDto> GetSummaryAsync(string? employeeIdForRestriction);
    }

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
            int page,
            int pageSize,
            string? employeeIdForRestriction)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            IQueryable<Ticket> tickets = _db.Tickets.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(employeeIdForRestriction)) // JWT
                tickets = tickets.Where(t => t.AssignedUserId == employeeIdForRestriction);
            else if (!string.IsNullOrWhiteSpace(assignedUserId)) // Parametre
                tickets = tickets.Where(x => x.AssignedUserId == assignedUserId);

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

            var totalCount = await tickets.CountAsync();
            var items = await tickets
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount, page, pageSize);
        }

        public async Task<Ticket?> GetByIdAsync(int id, string? employeeIdForRestriction)
        {
            var query = _db.Tickets
                .AsNoTracking()
                .Include(t => t.Notes)
                .Include(t => t.Histories)
                .Where(x => x.Id == id);

            if (!string.IsNullOrWhiteSpace(employeeIdForRestriction))
                query = query.Where(t => t.AssignedUserId == employeeIdForRestriction);

            var ticket = await query.FirstOrDefaultAsync();
            if (ticket == null)
                return null;

            ticket.Notes = ticket.Notes.OrderBy(n => n.CreatedAt).ToList();
            ticket.Histories = ticket.Histories.OrderBy(h => h.ChangedAt).ToList();
            return ticket;
        }

        public async Task<Ticket> CreateAsync(
            string title,
            string description,
            string customerName,
            string customerEmail,
            TicketPriority priority,
            string createdByUserId,
            string? assignedUserId)
        {
            if (string.IsNullOrWhiteSpace(createdByUserId))
                throw new InvalidOperationException("Oluşturan kullanıcı zorunlu.");

            if (!Enum.IsDefined(typeof(TicketPriority), priority))
                throw new InvalidOperationException("Geçersiz öncelik.");

            string? assignee = null;
            if (!string.IsNullOrWhiteSpace(assignedUserId))
                assignee = await EnsureEmployeeAsync(assignedUserId);

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
                    Status = assignee == null ? TicketStatus.New : TicketStatus.Assigned,
                    AssignedUserId = assignee,
                    CreatedByUserId = createdByUserId.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _db.Tickets.Add(ticket);

                try
                {
                    // Önce ticket, sonra history ekliyoruz ki ticket id'si history'de de kullanılabilir olsun. 
                    await _db.SaveChangesAsync();

                    AddHistory(ticket.Id, "Created", "Ticket", null, ticket.TicketNumber, createdByUserId);
                    if (assignee != null)
                        AddHistory(ticket.Id, "Assigned", "AssignedUserId", null, assignee, createdByUserId);

                    await _db.SaveChangesAsync();
                    return ticket;
                }
                // Unique çakışırsa numarayı yeniden üretip tekrar deniyoruz.
                catch (DbUpdateException)
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
            string customerEmail)
        {
            var ticket = await GetTrackedTicket(id);
            TicketStateMachine.EnsureCanModify(ticket);

            ticket.Title = title.Trim();
            ticket.Description = description.Trim();
            ticket.CustomerName = customerName.Trim();
            ticket.CustomerEmail = customerEmail.Trim();
            ticket.UpdatedAt = DateTime.UtcNow;

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

        public async Task<Ticket> ChangeStatusAsync(int id, TicketStatus newStatus, string actingUserId, bool isAdmin)
        {
            var ticket = await GetTrackedTicket(id);
            TicketStateMachine.EnsureCanModify(ticket);
            if (!isAdmin)
                TicketStateMachine.EnsureAssigneeCanUpdate(ticket, actingUserId);

            TicketStateMachine.EnsureTransition(ticket.Status, newStatus);

            var oldStatus = ticket.Status;
            ticket.Status = newStatus;
            ticket.UpdatedAt = DateTime.UtcNow;
            AddHistory(ticket.Id, "StatusChanged", "Status", oldStatus.ToString(), newStatus.ToString(), actingUserId);
            await _db.SaveChangesAsync();
            return ticket;
        }

        public async Task<Ticket> ChangePriorityAsync(int id, TicketPriority priority, string actingUserId)
        {
            if (!Enum.IsDefined(typeof(TicketPriority), priority))
                throw new InvalidOperationException("Geçersiz öncelik.");

            var ticket = await GetTrackedTicket(id);
            TicketStateMachine.EnsureCanModify(ticket);

            if (ticket.Priority == priority)
                return ticket;

            var oldPriority = ticket.Priority;
            ticket.Priority = priority;
            ticket.UpdatedAt = DateTime.UtcNow;
            AddHistory(ticket.Id, "PriorityChanged", "Priority", oldPriority.ToString(), priority.ToString(), actingUserId);
            await _db.SaveChangesAsync();
            return ticket;
        }

        public async Task<Ticket> AssignAsync(int id, string? assignedUserId, string actingUserId)
        {
            var ticket = await GetTrackedTicket(id);
            TicketStateMachine.EnsureCanModify(ticket);

            var oldAssignee = ticket.AssignedUserId;
            string? normalized = null;
            if (!string.IsNullOrWhiteSpace(assignedUserId))
                normalized = await EnsureEmployeeAsync(assignedUserId);

            if (oldAssignee != normalized)
            {
                ticket.AssignedUserId = normalized;
                ticket.UpdatedAt = DateTime.UtcNow;
                AddHistory(ticket.Id, "Assigned", "AssignedUserId", oldAssignee, ticket.AssignedUserId, actingUserId);
            }

            if (ticket.Status == TicketStatus.New && !string.IsNullOrWhiteSpace(ticket.AssignedUserId))
            {
                TicketStateMachine.EnsureTransition(ticket.Status, TicketStatus.Assigned);
                var oldStatus = ticket.Status;
                ticket.Status = TicketStatus.Assigned;
                ticket.UpdatedAt = DateTime.UtcNow;
                AddHistory(ticket.Id, "StatusChanged", "Status", oldStatus.ToString(), TicketStatus.Assigned.ToString(), actingUserId);
            }

            await _db.SaveChangesAsync();
            return ticket;
        }

        public async Task<TicketNote> AddNoteAsync(int ticketId, string noteText, string actingUserId, bool isAdmin)
        {
            if (string.IsNullOrWhiteSpace(noteText))
                throw new InvalidOperationException("Not boş olamaz.");

            var ticket = await GetTrackedTicket(ticketId);
            TicketStateMachine.EnsureCanModify(ticket);
            if (!isAdmin)
                TicketStateMachine.EnsureAssigneeCanUpdate(ticket, actingUserId);

            var note = new TicketNote
            {
                TicketId = ticketId,
                NoteText = noteText.Trim(),
                CreatedByUserId = actingUserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.TicketNotes.Add(note);
            ticket.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return note;
        }

        public async Task<TicketSummaryDto> GetSummaryAsync(string? employeeIdForRestriction)
        {
            IQueryable<Ticket> tickets = _db.Tickets.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(employeeIdForRestriction))
                tickets = tickets.Where(t => t.AssignedUserId == employeeIdForRestriction);

            // Bu işlemler ileride db'ye yaptırılabilir veya günlük adetleri kayıt eden bir reporter eklenebilir.
            var total = await tickets.CountAsync();
            var open = await tickets.CountAsync(t =>
                t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);
            var critical = await tickets.CountAsync(t => t.Priority == TicketPriority.Critical);
            var resolved = await tickets.CountAsync(t => t.Status == TicketStatus.Resolved);
            var closed = await tickets.CountAsync(t => t.Status == TicketStatus.Closed);

            return new TicketSummaryDto
            {
                Total = total,
                Open = open,
                Critical = critical,
                Resolved = resolved,
                Closed = closed
            };
        }

        private async Task<string> EnsureEmployeeAsync(string userId)
        {
            var id = userId.Trim();
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                throw new InvalidOperationException("Atanacak kullanıcı bulunamadı.");

            if (user.Role != UserRole.Employee)
                throw new InvalidOperationException("Atanacak kullanıcı Employee rolünde olmalı.");

            return id;
        }

        private async Task<Ticket> GetTrackedTicket(int id)
        {
            var ticket = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id);
            if (ticket == null)
                throw new KeyNotFoundException($"Ticket {id} bulunamadı.");
            return ticket;
        }

        private void AddHistory(
            int ticketId,
            string action,
            string fieldName,
            string? oldValue,
            string? newValue,
            string changedByUserId)
        {
            _db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticketId,
                Action = action,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedByUserId = changedByUserId,
                ChangedAt = DateTime.UtcNow
            });
        }
    }
}
