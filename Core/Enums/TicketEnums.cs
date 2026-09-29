namespace TicketManager.Enums
{
    public enum TicketStatus
    {
        New = 0,
        Assigned = 1,
        InProgress = 2,
        OnHold = 3,
        Resolved = 4,
        Closed = 5
    }

    public enum TicketPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    public enum UserRole
    {
        Admin = 0,
        Employee = 1
    }
}
