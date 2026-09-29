namespace TicketManager.Dtos
{
    public class TicketSummaryDto
    {
        public int Total { get; set; }
        public int Open { get; set; }
        public int Critical { get; set; }
        public int Resolved { get; set; }
        public int Closed { get; set; }
    }
}
