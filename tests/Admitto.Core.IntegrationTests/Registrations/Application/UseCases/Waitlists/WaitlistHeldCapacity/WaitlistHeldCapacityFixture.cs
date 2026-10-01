using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.WaitlistHeldCapacity;

/// <summary>
/// A sold-out, waitlist-enabled ticket type in WaitlistMode: <see cref="PublicCapacity"/> seats taken by registrations
/// of <see cref="RegisteredEmail"/>, and attendees waiting in queue order <see cref="WaitingEmail"/>.
/// </summary>
internal sealed class WaitlistHeldCapacityFixture
{
    private readonly List<RegistrationId> _registrationIds = [];
    private int _waitingCount;
    private bool _withAdminRegistration;

    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.New();
    public const int PublicCapacity = 2;
    public static EmailAddress AdminGuestEmail { get; } = EmailAddress.From("admin-guest@example.com");
    public RegistrationId AdminRegistrationId { get; private set; }
    public IReadOnlyList<RegistrationId> RegistrationIds => _registrationIds;

    public static EmailAddress RegisteredEmail(int index) => EmailAddress.From($"registered{index}@example.com");

    public static EmailAddress WaitingEmail(int position) => EmailAddress.From($"attendee{position}@example.com");

    private WaitlistHeldCapacityFixture()
    {
    }

    /// <summary>
    /// Sold out, with the given number of attendees waiting.
    /// </summary>
    public static WaitlistHeldCapacityFixture SoldOutWithWaitingEntries(int count) =>
        new() { _waitingCount = count };

    /// <summary>
    /// Sold out, with attendees waiting, and an admin registration for <see cref="AdminGuestEmail"/> holding an admin
    /// ticket on top of the public capacity.
    /// </summary>
    public static WaitlistHeldCapacityFixture SoldOutWithWaitingEntriesAndAdminRegistration(int count) =>
        new() { _waitingCount = count, _withAdminRegistration = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var now = DateTimeOffset.UtcNow;
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                EventId,
                TeamId,
                EventName.From("DevConf"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                now.AddDays(30),
                now.AddDays(31),
                TimeZoneId.From("UTC"));
            ticketedEvent.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
                now.AddDays(-1),
                now.AddDays(20)));
            ticketedEvent.ClearDomainEvents();
            dbContext.TicketedEvents.Add(ticketedEvent);

            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(
                TicketTypeId, TicketTypeName.From("Conference Pass"), [], PublicCapacity, waitlistEnabled: true);
            for (var i = 1; i <= PublicCapacity; i++)
            {
                var registration = Registration.Create(
                    TeamId,
                    EventId,
                    RegisteredEmail(i),
                    FirstName.From("Registered"),
                    LastName.From($"Attendee {i}"),
                    catalog.Claim([TicketTypeId], ClaimMode.Public)); // the last claim sells out → WaitlistMode
                registration.ClearDomainEvents();
                _registrationIds.Add(registration.Id);
                dbContext.Registrations.Add(registration);
            }

            if (_withAdminRegistration)
            {
                var registration = Registration.Create(
                    TeamId,
                    EventId,
                    AdminGuestEmail,
                    FirstName.From("Admin"),
                    LastName.From("Guest"),
                    catalog.Claim([TicketTypeId], ClaimMode.Admin));
                registration.ClearDomainEvents();
                AdminRegistrationId = registration.Id;
                dbContext.Registrations.Add(registration);
            }

            catalog.ClearDomainEvents();
            dbContext.TicketCatalogs.Add(catalog);

            var waitlist = Waitlist.Create(EventId, TicketTypeId, TeamId);
            for (var i = 1; i <= _waitingCount; i++)
                waitlist.AddEntry(WaitingEmail(i), now.AddMinutes(i), catalog);
            waitlist.ClearDomainEvents();
            dbContext.Waitlists.Add(waitlist);
        });
    }
}
