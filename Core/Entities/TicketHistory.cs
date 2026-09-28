namespace TicketManager.Entities
{
    public class TicketHistory
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string Action { get; set; } = null!;
        public string FieldName { get; set; } = null!;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }

        public Ticket Ticket { get; set; } = null!;
    }
}
