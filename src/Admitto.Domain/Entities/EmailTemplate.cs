namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a template for an email message.
/// </summary>
public class EmailTemplate : Aggregate
{
    private EmailTemplate()
    {
    }

    private EmailTemplate(
        Guid id,
        string type,
        string subject,
        string textBody,
        string htmlBody,
        Guid? teamId,
        Guid? ticketedEventId)
        : base(id)
    {
        Type = type;
        Subject = subject;
        TextBody = textBody;
        HtmlBody = htmlBody;
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
    }

    public string Type { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public string TextBody { get; private set; } = null!;
    public string HtmlBody { get; private set; } = null!;
    public Guid? TeamId { get; private set; }
    public Guid? TicketedEventId { get; private set; }

    public static EmailTemplate Create(
        string type,
        string subject,
        string textBody,
        string htmlBody,
        Guid? teamId = null,
        Guid? ticketedEventId = null)
    {
        return new EmailTemplate(
            Guid.NewGuid(),
            type,
            subject,
            textBody,
            htmlBody,
            teamId,
            ticketedEventId);
    }
}