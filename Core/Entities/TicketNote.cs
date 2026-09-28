namespace TicketManager.Entities
{
    public class TicketNote
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string NoteText { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public Ticket Ticket { get; set; } = null!;
    }
}
