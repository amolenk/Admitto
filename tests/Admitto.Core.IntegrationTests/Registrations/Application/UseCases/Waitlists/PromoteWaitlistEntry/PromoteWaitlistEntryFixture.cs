using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.PromoteWaitlistEntry;

internal sealed class PromoteWaitlistEntryFixture
{
    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId EarlyBirdId { get; } = TicketTypeId.New();
    public TicketTypeId WorkshopId { get; } = TicketTypeId.New();
    public RegistrationId VipRegistrationId { get; private set; }
    public WaitlistEntryId VipEntryId { get; private set; }

    public static EmailAddress FirstEmail { get; } = EmailAddress.From("first@example.com");
    public static EmailAddress VipEmail { get; } = EmailAddress.From("vip@example.com");
    public static EmailAddress ThirdEmail { get; } = EmailAddress.From("third@example.com");

    private PromoteWaitlistEntryFixture()
    {
    }

    /// <summary>
    /// A sold-out, waitlist-mode "Workshop" with three attendees queued; the VIP is second in line and holds a
    /// registration with an "Early Bird" ticket.
    /// </summary>
    public static PromoteWaitlistEntryFixture WithVipSecondInQueue() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                EventId,
                TeamId,
                EventName.From("DevConf 2026"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZoneId.From("UTC"));
            ticketedEvent.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(20)));
            ticketedEvent.ClearDomainEvents();
            dbContext.TicketedEvents.Add(ticketedEvent);

            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(EarlyBirdId, TicketTypeName.From("Early Bird"), [], 100);
            catalog.AddTicketType(WorkshopId, TicketTypeName.From("Workshop"), [], 1, waitlistEnabled: true, claimWindowHours: 8);
            catalog.Claim([EarlyBirdId], ClaimMode.Public);
            catalog.Claim([WorkshopId], ClaimMode.Public); // fills the last slot -> activates WaitlistMode
            catalog.ClearDomainEvents();
            dbContext.TicketCatalogs.Add(catalog);

            var waitlist = global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist.Create(
                EventId, WorkshopId, TeamId);
            var now = DateTimeOffset.UtcNow;
            waitlist.AddEntry(FirstEmail, now, catalog, RegistrationId.New());
            waitlist.AddEntry(VipEmail, now.AddMinutes(1), catalog, RegistrationId.New());
            waitlist.AddEntry(ThirdEmail, now.AddMinutes(2), catalog, RegistrationId.New());
            waitlist.ClearDomainEvents();
            VipEntryId = waitlist.Entries.Single(e => e.Email == VipEmail).Id;
            dbContext.Waitlists.Add(waitlist);

            var registration = Registration.Create(
                TeamId,
                EventId,
                VipEmail,
                FirstName.From("Vera"),
                LastName.From("Important"),
                [new TicketTypeSnapshot(EarlyBirdId, TicketTypeName.From("Early Bird"), [])],
                AdditionalDetails.From(new Dictionary<string, string>()));
            registration.ClearDomainEvents();
            VipRegistrationId = registration.Id;
            dbContext.Registrations.Add(registration);
        });
    }
}
