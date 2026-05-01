using Amolenk.Admitto.Module.Email.Application.Sending;
using Amolenk.Admitto.Module.Email.Application.Sending.Settings;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.SendTestEmail;
using Amolenk.Admitto.Module.Email.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Infrastructure.Security;
using Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Email.Tests.Application.UseCases.EmailSettings.SendTestEmail;

internal sealed class SendTestEmailFixture
{
    private const string ProtectedPasswordPlaintext = "secret";
    private bool _seedTeamSettings;
    private bool _seedEventSettings;
    private bool _eventSettingsIncomplete;

    private SendTestEmailFixture()
    {
    }

    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public IProtectedSecret ProtectedSecret { get; } = TestProtectedSecretFactory.Create();
    public FakeTestEmailSender EmailSender { get; } = new();

    public static SendTestEmailFixture TeamSettings() => new()
    {
        _seedTeamSettings = true
    };

    public static SendTestEmailFixture EventAndTeamSettings() => new()
    {
        _seedTeamSettings = true,
        _seedEventSettings = true
    };

    public static SendTestEmailFixture TeamSettingsOnly() => new()
    {
        _seedTeamSettings = true
    };

    public static SendTestEmailFixture IncompleteEventSettings() => new()
    {
        _seedEventSettings = true,
        _eventSettingsIncomplete = true
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment, CancellationToken ct)
    {
        await environment.Database.SeedAsync(db =>
        {
            if (_seedTeamSettings)
            {
                db.EmailSettings.Add(new EventEmailSettingsBuilder()
                    .ForTeam(TeamId)
                    .WithSmtpHost("team.smtp.example.com")
                    .WithFromAddress("team@example.com")
                    .Build());
            }

            if (_seedEventSettings)
            {
                db.EmailSettings.Add(new EventEmailSettingsBuilder()
                    .ForEvent(EventId)
                    .WithSmtpHost("event.smtp.example.com")
                    .WithFromAddress("event@example.com")
                    .WithBasicAuth(protectedPassword: ProtectedSecret.Protect(ProtectedPasswordPlaintext))
                    .Build());
            }
        }, ct);

        if (_eventSettingsIncomplete)
        {
            await environment.Database.Context.Database.ExecuteSqlRawAsync(
                "UPDATE email.email_settings SET protected_password = NULL WHERE scope_id = {0}",
                [EventId.Value],
                ct);
            environment.Database.Context.ChangeTracker.Clear();
        }
    }

    public SendTestEmailHandler CreateHandler(IntegrationTestEnvironment environment) =>
        new(environment.Database.Context, ProtectedSecret, EmailSender);

    public SendTestEmailCommand TeamCommand(string recipient = "ops@acme.org") =>
        new(EmailSettingsScope.Team, TeamId.Value, EmailAddress.From(recipient));

    public SendTestEmailCommand EventCommand(string recipient = "ops@acme.org") =>
        new(EmailSettingsScope.Event, EventId.Value, EmailAddress.From(recipient));
}

internal sealed class FakeTestEmailSender : IEmailSender
{
    public string Provider => "Fake";

    public List<(EffectiveEmailSettings Settings, EmailMessage Message)> SentMessages { get; } = [];
    public string? ExceptionMessage { get; set; }

    public ValueTask<string?> SendAsync(
        EffectiveEmailSettings settings,
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (ExceptionMessage is not null)
        {
            throw new InvalidOperationException(ExceptionMessage);
        }

        SentMessages.Add((settings, message));
        return ValueTask.FromResult<string?>("fake-message-id");
    }
}
