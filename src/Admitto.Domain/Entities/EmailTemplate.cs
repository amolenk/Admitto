using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a template for an email message.
/// </summary>
public class EmailTemplate : AggregateRoot
{
    private EmailTemplate()
    {
    }

    private EmailTemplate(
        Guid id,
        EmailType type,
        string subject,
        string body,
        Guid teamId,
        Guid? ticketedEventId)
        : base(id)
    {
        Type = type;
        Subject = subject;
        Body = body;
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
    }

    public EmailType Type { get; private set; }
    public string Subject { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public Guid TeamId { get; private set; }
    public Guid? TicketedEventId { get; private set; }

    public static EmailTemplate Create(
        EmailType type,
        string subject,
        string body,
        Guid teamId,
        Guid? ticketedEventId = null)
    {
        return new EmailTemplate(
            Guid.NewGuid(),
            type,
            subject,
            body,
            teamId,
            ticketedEventId);
    }
}