using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a list of recipients for bulk emails.
/// </summary>
public class EmailRecipientList : Aggregate
{
    private readonly List<EmailRecipient> _recipients = [];

    // EF Core constructor
    private EmailRecipientList()
    {
    }

    private EmailRecipientList(
        Guid id,
        Guid ticketedEventId,
        string name,
        List<EmailRecipient> recipients)
        : base(id)
    {
        Name = name;
        TicketedEventId = ticketedEventId;
        
        _recipients = recipients;
    }

    public Guid TicketedEventId { get; private set; }
    public string Name { get; private set; }
    
    public IReadOnlyCollection<EmailRecipient> Recipients => _recipients.AsReadOnly();

    public static EmailRecipientList Create(Guid ticketedEventId, string name, IEnumerable<EmailRecipient> recipients)
    {
        return new EmailRecipientList(
            Guid.NewGuid(),
            ticketedEventId,
            name,
            recipients.ToList());
    }
}