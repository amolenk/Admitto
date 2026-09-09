using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.DeliverEmail;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.DeliverEmail;

internal sealed class DeliverEmailFixture
{
    private readonly SystemEmailOptions _systemEmail;
    private readonly EmailDeliveryOptions _deliveryOptions;

    private DeliverEmailFixture(
        SystemEmailOptions systemEmail,
        EmailDeliveryOptions? deliveryOptions = null)
    {
        _systemEmail = systemEmail;
        _deliveryOptions = deliveryOptions ?? new EmailDeliveryOptions
        {
            InlineRetryDelay = TimeSpan.Zero
        };
        TeamId = TeamId.New();
        EventId = TicketedEventId.New();
        IdempotencyKey = $"deliver:{Guid.NewGuid():N}";
        Sender = new FakeEmailSender();
    }

    public TeamId TeamId { get; }
    public TicketedEventId EventId { get; }
    public string IdempotencyKey { get; }
    public FakeEmailSender Sender { get; }

    public static DeliverEmailFixture ValidTransport() => new(new SystemEmailOptions
    {
        SmtpHost = "smtp.example.com",
        SmtpPort = 587,
        FromAddress = "tickets@admitto.org",
        AuthMode = "None"
    });

    public static DeliverEmailFixture ValidTransportWithOptions(EmailDeliveryOptions deliveryOptions) =>
        new(new SystemEmailOptions
        {
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            FromAddress = "tickets@admitto.org",
            AuthMode = "None"
        }, deliveryOptions);

    public static DeliverEmailFixture InvalidBasicCredentials() => new(new SystemEmailOptions
    {
        SmtpHost = "smtp.example.com",
        SmtpPort = 587,
        FromAddress = "tickets@admitto.org",
        AuthMode = "Basic",
        Username = "smtp-user"
    });

    public static DeliverEmailFixture InvalidSecurityCombination() => new(new SystemEmailOptions
    {
        SmtpHost = "smtp.example.com",
        SmtpPort = 587,
        SmtpSsl = true,
        SmtpStartTls = true,
        FromAddress = "tickets@admitto.org",
        AuthMode = "None"
    });

    public static DeliverEmailFixture MissingHostAndFrom() => new(new SystemEmailOptions
    {
        AuthMode = "None"
    });

    public async ValueTask SeedClaimAsync(
        IntegrationTestEnvironment environment,
        EmailLogStatus status = EmailLogStatus.Pending,
        DateTimeOffset? sentAt = null)
    {
        var now = DateTimeOffset.UtcNow;
        await environment.EmailDatabase.SeedAsync(db => db.EmailLog.Add(EmailLog.Create(
            TeamId,
            EventId,
            IdempotencyKey,
            EmailAddress.From("alice@example.com"),
            BuiltInEmailTemplateNames.TicketConfirmation,
            "Subject",
            status,
            sentAt,
            statusUpdatedAt: now)));
    }

    public DeliverEmailHandler BuildHandler(IntegrationTestEnvironment environment) =>
        new(
            environment.EmailDatabase.Context,
            new SmtpTransportSettingsResolver(Options.Create(_systemEmail)),
            Sender,
            new Outbox(environment.EmailDatabase.Context),
            new StaticOptionsMonitor<EmailDeliveryOptions>(_deliveryOptions));

    public DeliverEmailCommand Command() => new(
        TeamId.Value,
        EventId.Value,
        "alice@example.com",
        "Alice",
        BuiltInEmailTemplateNames.TicketConfirmation,
        IdempotencyKey,
        "Subject",
        "Text",
        "<p>Html</p>");

    internal sealed class FakeEmailSender : IEmailSender
    {
        public string Provider => "Fake";
        public int SendAttempts { get; private set; }
        public bool ShouldThrow { get; set; }

        public ValueTask<string?> SendAsync(
            SmtpTransportSettings settings,
            EmailMessage message,
            CancellationToken cancellationToken = default)
        {
            SendAttempts++;
            if (ShouldThrow)
                throw new InvalidOperationException("SMTP error (fake)");

            return ValueTask.FromResult<string?>("message-id");
        }
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
