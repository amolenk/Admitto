using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Domain.Entities;

/// <summary>
/// Unified SMTP/email server settings aggregate keyed by <see cref="EmailSettingsScope"/> and
/// <see cref="ScopeId"/>. Supports both team-scoped and event-scoped configurations.
/// </summary>
public class EmailSettings : Aggregate<EmailSettingsId>
{
    // Required for EF Core
    private EmailSettings()
    {
    }

    private EmailSettings(
        EmailSettingsId id,
        EmailSettingsScope scope,
        Guid scopeId,
        Hostname smtpHost,
        Port smtpPort,
        EmailAddress fromAddress,
        EmailAuthMode authMode,
        SmtpUsername? username,
        ProtectedPassword? protectedPassword)
        : base(id)
    {
        Scope = scope;
        ScopeId = scopeId;
        SmtpHost = smtpHost;
        SmtpPort = smtpPort;
        FromAddress = fromAddress;
        AuthMode = authMode;
        Username = username;
        ProtectedPassword = protectedPassword;
    }

    public EmailSettingsScope Scope { get; private set; }
    public Guid ScopeId { get; private set; }
    public Hostname SmtpHost { get; private set; }
    public Port SmtpPort { get; private set; }
    public EmailAddress FromAddress { get; private set; }
    public EmailAuthMode AuthMode { get; private set; }
    public SmtpUsername? Username { get; private set; }

    /// <summary>
    /// Encrypted password produced by <c>IProtectedSecret</c>. Never contains plaintext.
    /// </summary>
    public ProtectedPassword? ProtectedPassword { get; private set; }

    public static EmailSettings Create(
        EmailSettingsScope scope,
        Guid scopeId,
        Hostname smtpHost,
        Port smtpPort,
        EmailAddress fromAddress,
        EmailAuthMode authMode,
        SmtpUsername? username,
        ProtectedPassword? protectedPassword)
    {
        EnsureBasicAuthHasCredentials(authMode, username, protectedPassword);

        return new EmailSettings(
            EmailSettingsId.New(),
            scope,
            scopeId,
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
