using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.CancelRegistration;

internal sealed class CancelRegistrationFixture
{
    private bool _preCancel;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    private CancelRegistrationFixture()
    {
    }

    public static CancelRegistrationFixture ActiveRegistration() => new();

    public static CancelRegistrationFixture AlreadyCancelled() => new() { _preCancel = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.Database.SeedAsync(dbContext =>
        {
            var registration = Registration.Create(
                TeamId,
                EventId,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Test"),
                [new TicketTypeSnapshot("general-admission", "general-admission", [])]);

            RegistrationId = registration.Id;

            if (_preCancel)
            {
                registration.Cancel(CancellationReason.AttendeeRequest);
            }

            dbContext.Registrations.Add(registration);
        });
    }
}
