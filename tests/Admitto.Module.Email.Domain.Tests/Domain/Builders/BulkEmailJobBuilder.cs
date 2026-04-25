using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Domain.Tests.Builders;

public sealed class BulkEmailJobBuilder
{
    private TeamId _teamId = TeamId.New();
    private TicketedEventId _eventId = TicketedEventId.New();
    private string _emailType = EmailTemplateType.Reconfirm;
    private string? _subject;
    private string? _textBody;
    private string? _htmlBody;
    private BulkEmailJobSource _source = new AttendeeSource(new QueryRegistrationsDto());
    private EmailAddress _triggeredBy = EmailAddress.From("admin@example.com");
    private bool _systemTriggered;
    private DateTimeOffset _now = DateTimeOffset.UtcNow;

    public BulkEmailJobBuilder ForTeam(TeamId teamId) { _teamId = teamId; return this; }
    public BulkEmailJobBuilder ForEvent(TicketedEventId eventId) { _eventId = eventId; return this; }
    public BulkEmailJobBuilder WithEmailType(string type) { _emailType = type; return this; }
    public BulkEmailJobBuilder WithAdHocBodies(string? subject, string? text, string? html)
    { _subject = subject; _textBody = text; _htmlBody = html; return this; }
    public BulkEmailJobBuilder WithSource(BulkEmailJobSource source) { _source = source; return this; }
    public BulkEmailJobBuilder TriggeredBy(string email) { _triggeredBy = EmailAddress.From(email); return this; }
    public BulkEmailJobBuilder AsSystemTriggered() { _systemTriggered = true; return this; }
    public BulkEmailJobBuilder At(DateTimeOffset now) { _now = now; return this; }

    public BulkEmailJob Build() =>
        _systemTriggered
            ? BulkEmailJob.CreateSystemTriggered(
                _teamId, _eventId, _emailType, _subject, _textBody, _htmlBody, _source, _now)
            : BulkEmailJob.Create(
                _teamId, _eventId, _emailType, _subject, _textBody, _htmlBody, _source, _triggeredBy, _now);

    public static BulkEmailRecipient Recipient(string email, string? displayName = null) =>
        new(email, displayName, registrationId: null, parametersJson: "{}");
}
