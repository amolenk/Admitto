using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.HandleReconfirmAutoExpired;

internal sealed class HandleReconfirmAutoExpiredFixture
{
    private bool _cancelled;
    private bool _reconfirmed;

    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId TicketedEventId { get; } = TicketedEventId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    private HandleReconfirmAutoExpiredFixture() { }

    public static HandleReconfirmAutoExpiredFixture ActiveRegistration() => new();
    public static HandleReconfirmAutoExpiredFixture CancelledRegistration() => new() { _cancelled = true };
    public static HandleReconfirmAutoExpiredFixture ReconfirmedRegistration() => new() { _reconfirmed = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        Registration? seeded = null;

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var registration = Registration.Create(
                TeamId,
                TicketedEventId,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Test"),
                [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("General"), [])]);

            if (_reconfirmed)
            {
                registration.Reconfirm(DateTimeOffset.UtcNow);
            }

            if (_cancelled)
            {
                registration.Cancel(CancellationReason.AttendeeRequest);
            }

            dbContext.Registrations.Add(registration);
            seeded = registration;
        });

        RegistrationId = seeded!.Id;
    }
}
