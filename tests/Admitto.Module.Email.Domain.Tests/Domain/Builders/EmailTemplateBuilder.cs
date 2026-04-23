using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Domain.Tests.Builders;

public class EmailTemplateBuilder
{
    public static readonly TicketedEventId DefaultEventId = TicketedEventId.New();

    private EmailSettingsScope _scope = EmailSettingsScope.Event;
    private Guid _scopeId = DefaultEventId.Value;
    private string _type = EmailTemplateType.Ticket;
    private string _subject = "Your ticket";
    private string _textBody = "Hello {{ first_name }}";
    private string _htmlBody = "<p>Hello {{ first_name }}</p>";

    public EmailTemplateBuilder ForEvent(TicketedEventId id) { _scopeId = id.Value; _scope = EmailSettingsScope.Event; return this; }
    public EmailTemplateBuilder ForTeam(TeamId id) { _scopeId = id.Value; _scope = EmailSettingsScope.Team; return this; }
    public EmailTemplateBuilder WithType(string type) { _type = type; return this; }
    public EmailTemplateBuilder WithSubject(string subject) { _subject = subject; return this; }
    public EmailTemplateBuilder WithTextBody(string body) { _textBody = body; return this; }
    public EmailTemplateBuilder WithHtmlBody(string body) { _htmlBody = body; return this; }

    public EmailTemplate Build() =>
        EmailTemplate.Create(_scope, _scopeId, _type, _subject, _textBody, _htmlBody);
}
