using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Domain.Entities;

/// <summary>
/// Per-event SMTP/email server settings owned by the Email module.
/// </summary>
/// <remarks>
/// The aggregate's identifier IS the owning <see cref="TicketedEventId"/>. There is at most one
/// <see cref="EventEmailSettings"/> per event, so the database PK enforces the uniqueness invariant.
/// </remarks>
public class EventEmailSettings : Aggregate<TicketedEventId>
{
    // ReSharper disable once UnusedMember.Local
    // Required for EF Core
    private EventEmailSettings()
    {
    }

    private EventEmailSettings(
        TicketedEventId ticketedEventId,
        Hostname smtpHost,
        Port smtpPort,
        EmailAddress fromAddress,
        EmailAuthMode authMode,
        SmtpUsername? username,
        ProtectedPassword? protectedPassword)
        : base(ticketedEventId)
    {
        SmtpHost = smtpHost;
        SmtpPort = smtpPort;
        FromAddress = fromAddress;
        AuthMode = authMode;
        Username = username;
        ProtectedPassword = protectedPassword;
    }

    public Hostname SmtpHost { get; private set; }
    public Port SmtpPort { get; private set; }
    public EmailAddress FromAddress { get; private set; }
    public EmailAuthMode AuthMode { get; private set; }
    public SmtpUsername? Username { get; private set; }

    /// <summary>
    /// Encrypted password produced by <c>IProtectedSecret</c>. Never contains plaintext.
    /// </summary>
    public ProtectedPassword? ProtectedPassword { get; private set; }

    public static EventEmailSettings Create(
        TicketedEventId ticketedEventId,
        Hostname smtpHost,
        Port smtpPort,
        EmailAddress fromAddress,
        EmailAuthMode authMode,
        SmtpUsername? username,
        ProtectedPassword? protectedPassword)
    {
        EnsureBasicAuthHasCredentials(authMode, username, protectedPassword);

        return new EventEmailSettings(
            ticketedEventId,
            smtpHost,
            smtpPort,
            fromAddress,
            authMode,
            authMode == EmailAuthMode.Basic ? username : null,
            authMode == EmailAuthMode.Basic ? protectedPassword : null);
    }

    /// <summary>
    /// Updates the settings. Pass <see langword="null"/> for any field that should remain unchanged.
    /// To leave the encrypted password unchanged, omit <paramref name="protectedPassword"/>
    /// (i.e. pass <see langword="null"/>).
    /// </summary>
    public void Update(
        Hostname? smtpHost,
        Port? smtpPort,
        EmailAddress? fromAddress,
        EmailAuthMode? authMode,
        SmtpUsername? username,
        ProtectedPassword? protectedPassword)
    {
        if (smtpHost.HasValue) SmtpHost = smtpHost.Value;
        if (smtpPort.HasValue) SmtpPort = smtpPort.Value;
        if (fromAddress.HasValue) FromAddress = fromAddress.Value;

        if (authMode.HasValue)
        {
            AuthMode = authMode.Value;
            if (AuthMode == EmailAuthMode.None)
            {
                Username = null;
                ProtectedPassword = null;
            }
        }

        if (AuthMode == EmailAuthMode.Basic)
        {
            if (username.HasValue) Username = username.Value;
            if (protectedPassword.HasValue) ProtectedPassword = protectedPassword.Value;
        }

        EnsureBasicAuthHasCredentials(AuthMode, Username, ProtectedPassword);
    }

    /// <summary>
    /// Returns true when all minimum required fields for outbound mail are populated.
    /// Used by <c>IEventEmailFacade.IsEmailConfiguredAsync</c>.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(SmtpHost.Value)) return false;
        if (string.IsNullOrWhiteSpace(FromAddress.Value)) return false;

        return AuthMode switch
        {
            EmailAuthMode.None => true,
            EmailAuthMode.Basic =>
                Username.HasValue
                && !string.IsNullOrWhiteSpace(Username.Value.Value)
                && ProtectedPassword.HasValue
                && !string.IsNullOrWhiteSpace(ProtectedPassword.Value.Ciphertext),
            _ => false
        };
    }

    private static void EnsureBasicAuthHasCredentials(
        EmailAuthMode authMode,
        SmtpUsername? username,
        ProtectedPassword? protectedPassword)
    {
        if (authMode != EmailAuthMode.Basic) return;

        if (username is null || string.IsNullOrWhiteSpace(username.Value.Value)
            || protectedPassword is null || string.IsNullOrWhiteSpace(protectedPassword.Value.Ciphertext))
        {
            throw new BusinessRuleViolationException(Errors.BasicAuthRequiresCredentials);
        }
    }

    internal static class Errors
    {
        public static readonly Error BasicAuthRequiresCredentials = new(
            "event_email_settings.basic_auth_requires_credentials",
            "Basic authentication requires both a username and a password.");
    }
}
