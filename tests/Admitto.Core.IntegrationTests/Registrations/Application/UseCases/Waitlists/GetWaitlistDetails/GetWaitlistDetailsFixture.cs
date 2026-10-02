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

    private bool _waitlistEnabled = true;
    private bool _withEntry;
    private bool _withPendingNotification;

    /// <summary>
    /// A waitlist with one active entry whose email, first name, and last name match a real registration.
    /// </summary>
    public static GetWaitlistDetailsFixture WithOneActiveEntry() => new() { _withEntry = true };

    /// <summary>
    /// A waitlist with one entry holding an outstanding coupon offer whose email matches a real registration.
    /// </summary>
    public static GetWaitlistDetailsFixture WithOnePendingNotification() =>
        new() { _withEntry = true, _withPendingNotification = true };

    /// <summary>
    /// A ticket type with waitlisting enabled, but no entries and no waitlist aggregate created yet
    /// (it has never gone into waitlist mode).
    /// </summary>
    public static GetWaitlistDetailsFixture WithWaitlistEnabledAndNoEntries() => new();

    /// <summary>
    /// A ticket type that has never had waitlisting enabled.
    /// </summary>
    public static GetWaitlistDetailsFixture WithWaitlistNotEnabled() => new() { _waitlistEnabled = false };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(
                TicketTypeId, TicketTypeName.From("Workshop"), [], 1, waitlistEnabled: _waitlistEnabled);

            if (_waitlistEnabled)
            {
                catalog.Claim([TicketTypeId], ClaimMode.Public); // sold out
            }

            catalog.ClearDomainEvents();
            dbContext.TicketCatalogs.Add(catalog);

            if (!_withEntry)
                return;

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
            var entry = waitlist.Entries.First();

            if (_withPendingNotification)
            {
                var ticketedEvent = TicketedEvent.Create(
                    CreationRequestId.From(Guid.NewGuid()),
                    EventId,
                    TeamId,
                    EventName.From("Conference"),
                    AbsoluteUrl.From("https://example.com"),
                    AbsoluteUrl.From("https://tickets.example.com"),
                    DateTimeOffset.UtcNow.AddDays(30),
                    DateTimeOffset.UtcNow.AddDays(31),
                    TimeZoneId.From("UTC"));
                ticketedEvent.ClearDomainEvents();
                dbContext.TicketedEvents.Add(ticketedEvent);

                var coupon = waitlist.IssueCouponToEntry(entry.Id, ticketedEvent, catalog, DateTimeOffset.UtcNow);
                dbContext.Coupons.Add(coupon);
            }

            catalog.ClearDomainEvents();
            waitlist.ClearDomainEvents();
            dbContext.Waitlists.Add(waitlist);
        });
    }
}
