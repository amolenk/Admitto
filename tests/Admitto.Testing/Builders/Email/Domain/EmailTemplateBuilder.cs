using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Testing.Builders.Email.Domain;

public class EmailTemplateBuilder
{
    public static readonly TeamId DefaultTeamId = TeamId.New();
    public static readonly TicketedEventId DefaultEventId = TicketedEventId.New();

    private TeamId _teamId = DefaultTeamId;
    private TicketedEventId? _eventId = DefaultEventId;
    private string _name = BuiltInEmailTemplateNames.TicketConfirmation;
    private string _subject = "Your ticket";
    private string _textBody = "Hello {{ first_name }}";
    private string _htmlBody = "<p>Hello {{ first_name }}</p>";

    public EmailTemplateBuilder ForEvent(TicketedEventId id) { _eventId = id; return this; }
    public EmailTemplateBuilder ForTeam(TeamId id) { _teamId = id; _eventId = null; return this; }
    public EmailTemplateBuilder ForTeamAndEvent(TeamId teamId, TicketedEventId eventId) { _teamId = teamId; return ForEvent(eventId); }
    public EmailTemplateBuilder WithName(string name) { _name = name; return this; }
    public EmailTemplateBuilder WithSubject(string subject) { _subject = subject; return this; }
    public EmailTemplateBuilder WithTextBody(string body) { _textBody = body; return this; }
    public EmailTemplateBuilder WithHtmlBody(string body) { _htmlBody = body; return this; }

    public EmailTemplate Build() =>
        EmailTemplate.Create(_teamId, _eventId, _name, _subject, _textBody, _htmlBody);
}
