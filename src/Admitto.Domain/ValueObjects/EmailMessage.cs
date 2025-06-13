namespace Amolenk.Admitto.Domain.ValueObjects;

public class EmailMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string RecipientEmail { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required TeamId TeamId { get; init; }
    public TicketedEventId? TicketedEventId { get; init; }
    public AttendeeId? AttendeeId { get; init; }
}
