using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;

namespace Amolenk.Admitto.Core.Email.Domain.Entities;

public class EmailTemplate : Aggregate<EmailTemplateId>
{
    // Required for EF Core
    private EmailTemplate()
    {
    }

    private EmailTemplate(
        EmailTemplateId id,
        TeamId teamId,
        TicketedEventId? ticketedEventId,
        string name,
        string subject,
        string textBody,
        string? htmlBody)
        : base(id)
    {
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
        Name = name;
        Subject = subject;
        TextBody = textBody;
        HtmlBody = htmlBody;
    }

    public TeamId TeamId { get; private set; }
    public TicketedEventId? TicketedEventId { get; private set; }

    /// <summary>
    /// The template's unique name within its scope.
    /// Names matching <see cref="BuiltInEmailTemplateNames"/> are reserved for built-in templates
    /// and may not be used for custom templates or renamed.
    /// </summary>
    public string Name { get; private set; } = default!;

    public string Subject { get; private set; } = default!;
    public string TextBody { get; private set; } = default!;
    public string? HtmlBody { get; private set; }

    public static EmailTemplate Create(
        TeamId teamId,
        TicketedEventId? ticketedEventId,
        string name,
        string subject,
        string textBody,
        string? htmlBody)
    {
        return new EmailTemplate(
            EmailTemplateId.New(),
            teamId,
            ticketedEventId,
            name,
            subject,
            textBody,
            htmlBody);
    }

    public void Update(string subject, string textBody, string? htmlBody)
    {
        Subject = subject;
        TextBody = textBody;
        HtmlBody = htmlBody;
    }

    public void Rename(string newName)
    {
        Name = newName;
    }
}
