using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email;

public class EmailMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string RecipientEmail { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required TeamId TeamId { get; init; }
    public required TicketedEventId? TicketedEventId { get; init; }
    public required AttendeeId? AttendeeId { get; init; }
}
