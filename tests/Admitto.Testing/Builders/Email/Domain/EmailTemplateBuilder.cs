using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Testing.Builders.Email.Domain;

public sealed class EmailTemplateBuilder
{
    private TeamId _teamId = TeamId.New();
    private TicketedEventId? _eventId;
    private string _name = "Test template";
    private string _subject = "Hello {{ first_name }}";
    private string _textBody = "Hello {{ first_name }}";
    private string? _htmlBody = "<p>Hello {{ first_name }}</p>";

    public EmailTemplateBuilder ForTeam(TeamId teamId) { _teamId = teamId; _eventId = null; return this; }
    public EmailTemplateBuilder ForTeamAndEvent(TeamId teamId, TicketedEventId eventId) { _teamId = teamId; _eventId = eventId; return this; }
    public EmailTemplateBuilder WithName(string name) { _name = name; return this; }
    public EmailTemplateBuilder WithSubject(string subject) { _subject = subject; return this; }
    public EmailTemplateBuilder WithTextBody(string textBody) { _textBody = textBody; return this; }
    public EmailTemplateBuilder WithHtmlBody(string? htmlBody) { _htmlBody = htmlBody; return this; }

    public EmailTemplate Build() => EmailTemplate.Create(_teamId, _eventId, _name, _subject, _textBody, _htmlBody);
}
