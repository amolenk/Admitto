using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Testing.Builders.Email.Domain;

public class EmailTemplateBuilder
{
    public static readonly TicketedEventId DefaultEventId = TicketedEventId.New();

    private EmailSettingsScope _scope = EmailSettingsScope.Event;
    private EmailScopeId _scopeId = EmailScopeId.From(DefaultEventId.Value);
    private string _name = BuiltInEmailTemplateNames.TicketConfirmation;
    private string _subject = "Your ticket";
    private string _textBody = "Hello {{ first_name }}";
    private string _htmlBody = "<p>Hello {{ first_name }}</p>";

    public EmailTemplateBuilder ForEvent(TicketedEventId id) { _scopeId = EmailScopeId.From(id.Value); _scope = EmailSettingsScope.Event; return this; }
    public EmailTemplateBuilder ForTeam(TeamId id) { _scopeId = EmailScopeId.From(id.Value); _scope = EmailSettingsScope.Team; return this; }
    public EmailTemplateBuilder WithName(string name) { _name = name; return this; }
    public EmailTemplateBuilder WithSubject(string subject) { _subject = subject; return this; }
    public EmailTemplateBuilder WithTextBody(string body) { _textBody = body; return this; }
    public EmailTemplateBuilder WithHtmlBody(string body) { _htmlBody = body; return this; }

    public EmailTemplate Build() =>
        EmailTemplate.Create(_scope, _scopeId, _name, _subject, _textBody, _htmlBody);
}
