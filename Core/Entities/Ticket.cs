using TicketManager.Enums;

namespace TicketManager.Entities
{
    public class Ticket
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;
        public TicketPriority Priority { get; set; }
        public TicketStatus Status { get; set; }
        public string? AssignedUserId { get; set; }
        public string? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<TicketNote> Notes { get; set; } = new List<TicketNote>();
        public ICollection<TicketHistory> Histories { get; set; } = new List<TicketHistory>();
    }
}
