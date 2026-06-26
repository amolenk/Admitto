using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Testing.Builders.Email.Domain;

public class EventEmailSettingsBuilder
{
    public static readonly TeamId DefaultTeamId = TeamId.New();
    public static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    public const string DefaultSmtpHost = "smtp.example.com";
    public const int DefaultSmtpPort = 587;
    public static readonly EmailAddress DefaultFromAddress = EmailAddress.From("noreply@example.com");

    private TeamId _teamId = DefaultTeamId;
    private Hostname _smtpHost = Hostname.From(DefaultSmtpHost);
    private Port _smtpPort = Port.From(DefaultSmtpPort);
    private EmailAddress _fromAddress = DefaultFromAddress;
    private EmailAuthMode _authMode = EmailAuthMode.None;
    private SmtpUsername? _username;
    private ProtectedPassword? _protectedPassword;
    private EmailAccentColor _accentColor = EmailAccentColor.From(EmailSettings.DefaultAccentColor);
    private EmailFontFamily _fontFamily = EmailFontFamily.From(EmailSettings.DefaultFontFamily);

    public EventEmailSettingsBuilder ForEvent(TicketedEventId id) { return this; }
    public EventEmailSettingsBuilder ForTeam(TeamId id) { _teamId = id; return this; }
    public EventEmailSettingsBuilder ForTeamAndEvent(TeamId teamId, TicketedEventId eventId) { _teamId = teamId; return this; }
    public EventEmailSettingsBuilder WithSmtpHost(string host) { _smtpHost = Hostname.From(host); return this; }
    public EventEmailSettingsBuilder WithSmtpPort(int port) { _smtpPort = Port.From(port); return this; }
    public EventEmailSettingsBuilder WithFromAddress(string address) { _fromAddress = EmailAddress.From(address); return this; }
    public EventEmailSettingsBuilder WithBranding(string accentColor, string fontFamily)
    { _accentColor = EmailAccentColor.From(accentColor); _fontFamily = EmailFontFamily.From(fontFamily); return this; }

    public EventEmailSettingsBuilder WithBasicAuth(string username = "user", string protectedPassword = "ENCRYPTED:secret")
    {
        _authMode = EmailAuthMode.Basic;
        _username = SmtpUsername.From(username);
        _protectedPassword = ProtectedPassword.FromCiphertext(protectedPassword);
        return this;
    }

    public EmailSettings Build() =>
        EmailSettings.Create(_teamId, _smtpHost, _smtpPort, _fromAddress, _authMode, _username, _protectedPassword, _accentColor, _fontFamily);
}
