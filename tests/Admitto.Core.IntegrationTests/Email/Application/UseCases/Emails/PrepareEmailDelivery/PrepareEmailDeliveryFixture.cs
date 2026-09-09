using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.DeliverEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using RegistrationCycleIdValue = Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects.RegistrationCycleId;
using RegistrationIdValue = Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects.RegistrationId;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery;

internal sealed class PrepareEmailDeliveryFixture
{
    private PrepareEmailDeliveryFixture()
    {
        TeamId = TeamId.New();
        EventId = TicketedEventId.New();
        RegistrationId = Guid.NewGuid();
        RegistrationCycleId = Guid.NewGuid();
        IdempotencyKey = $"prepare:{Guid.NewGuid():N}";
    }

    public TeamId TeamId { get; }
    public TicketedEventId EventId { get; }
    public Guid RegistrationId { get; }
    public Guid RegistrationCycleId { get; }
    public string IdempotencyKey { get; }

    public static PrepareEmailDeliveryFixture RenderedEmail() => new();

    public PrepareEmailDeliveryCommand Command() => new(
        TeamId.Value,
        EventId.Value,
        "alice@example.com",
        "Alice",
        BuiltInEmailTemplateNames.TicketConfirmation,
        IdempotencyKey,
        "Rendered subject",
        "Rendered text",
        "<p>Rendered html</p>",
        RegistrationId,
        RegistrationCycleId);

    public PrepareEmailDeliveryHandler BuildHandler(IntegrationTestEnvironment environment) =>
        new(
            environment.EmailDatabase.Context,
            new Outbox(environment.EmailDatabase.Context));

    public async ValueTask SeedClaimAsync(
        IntegrationTestEnvironment environment,
        EmailLogStatus status,
        DateTimeOffset? sentAt = null)
    {
        var now = DateTimeOffset.UtcNow;
        await environment.EmailDatabase.SeedAsync(db => db.EmailLog.Add(EmailLog.Create(
            TeamId,
            EventId,
            IdempotencyKey,
            EmailAddress.From("alice@example.com"),
            BuiltInEmailTemplateNames.TicketConfirmation,
            "Rendered subject",
            status,
            sentAt,
            now,
            registrationId: RegistrationIdValue.From(RegistrationId),
            registrationCycleId: RegistrationCycleIdValue.From(RegistrationCycleId))));
    }

    public PrepareEmailDeliveryCommand CommandWithoutRegistrationMetadata() => new(
        TeamId.Value,
        EventId.Value,
        "alice@example.com",
        "Alice",
        BuiltInEmailTemplateNames.TicketConfirmation,
        $"{IdempotencyKey}:without-registration",
        "Rendered subject",
        "Rendered text",
        "<p>Rendered html</p>");
}
