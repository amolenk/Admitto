using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Domain.Tests.Builders;

public class EventEmailSettingsBuilder
{
    public static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    public const string DefaultSmtpHost = "smtp.example.com";
    public const int DefaultSmtpPort = 587;
    public static readonly EmailAddress DefaultFromAddress = EmailAddress.From("noreply@example.com");

    private Guid _scopeId = DefaultEventId.Value;
    private EmailSettingsScope _scope = EmailSettingsScope.Event;
    private Hostname _smtpHost = Hostname.From(DefaultSmtpHost);
    private Port _smtpPort = Port.From(DefaultSmtpPort);
    private EmailAddress _fromAddress = DefaultFromAddress;
    private EmailAuthMode _authMode = EmailAuthMode.None;
    private SmtpUsername? _username;
    private ProtectedPassword? _protectedPassword;

    public EventEmailSettingsBuilder ForEvent(TicketedEventId id) { _scopeId = id.Value; _scope = EmailSettingsScope.Event; return this; }
    public EventEmailSettingsBuilder ForTeam(TeamId id) { _scopeId = id.Value; _scope = EmailSettingsScope.Team; return this; }
    public EventEmailSettingsBuilder WithSmtpHost(string host) { _smtpHost = Hostname.From(host); return this; }
    public EventEmailSettingsBuilder WithSmtpPort(int port) { _smtpPort = Port.From(port); return this; }
    public EventEmailSettingsBuilder WithFromAddress(string address) { _fromAddress = EmailAddress.From(address); return this; }

    public EventEmailSettingsBuilder WithBasicAuth(string username = "user", string protectedPassword = "ENCRYPTED:secret")
    {
        _authMode = EmailAuthMode.Basic;
        _username = SmtpUsername.From(username);
        _protectedPassword = ProtectedPassword.FromCiphertext(protectedPassword);
        return this;
    }

    public EmailSettings Build() =>
        EmailSettings.Create(_scope, _scopeId, _smtpHost, _smtpPort, _fromAddress, _authMode, _username, _protectedPassword);
}
