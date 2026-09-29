using TicketManager.Enums;

namespace TicketManager.Entities
{
    public class TicketNote
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string NoteText { get; set; } = null!;
        public string CreatedByUserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public Status Status { get; set; } = Status.Active;

        public Ticket Ticket { get; set; } = null!;
    }
}
