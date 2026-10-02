using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.GetWaitlistDetails;

internal sealed class GetWaitlistDetailsFixture
{
    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.New();
    public RegistrationId RegistrationId { get; private set; }
    public EmailAddress Email { get; } = EmailAddress.From("alice@example.com");

    private GetWaitlistDetailsFixture()
    {
    }

    /// <summary>
    /// A waitlist with one active entry whose email, first name, and last name match a real registration.
    /// </summary>
    public static GetWaitlistDetailsFixture WithOneActiveEntry() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(TicketTypeId, TicketTypeName.From("Workshop"), [], 1, waitlistEnabled: true);
            catalog.Claim([TicketTypeId], ClaimMode.Public); // sold out
            catalog.ClearDomainEvents();
            dbContext.TicketCatalogs.Add(catalog);

            var registration = Registration.Create(
                TeamId,
                EventId,
                Email,
                FirstName.From("Alice"),
                LastName.From("Doe"),
                [],
                AdditionalDetails.From(new Dictionary<string, string>()),
                waitlistedTickets: [new TicketTypeSnapshot(TicketTypeId, TicketTypeName.From("Workshop"), [])]);
            registration.ClearDomainEvents();
            RegistrationId = registration.Id;
            dbContext.Registrations.Add(registration);

            var waitlist = Waitlist.Create(EventId, TicketTypeId, TeamId);
            waitlist.AddEntry(Email, DateTimeOffset.UtcNow, catalog, RegistrationId);
            waitlist.ClearDomainEvents();
            dbContext.Waitlists.Add(waitlist);
        });
    }
}
