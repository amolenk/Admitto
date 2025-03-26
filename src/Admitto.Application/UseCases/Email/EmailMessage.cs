using System.Collections.ObjectModel;

namespace Amolenk.Admitto.Application.UseCases.Email;

public record EmailTemplateParameter(string Name, string Value);

public class EmailTemplateParameters : Collection<EmailTemplateParameter> {}

public class EmailMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid? TicketedEventId { get; init; }
    public string TemplateId { get; init; } = null!;
    // EF Core doesn't support storing Dictionary<string, string> directly in a JSON column
    public EmailTemplateParameters TemplateParameters { get; init; } = [];
    public required string RecipientEmail { get; init; }
    public bool Priority { get; init; }
}
