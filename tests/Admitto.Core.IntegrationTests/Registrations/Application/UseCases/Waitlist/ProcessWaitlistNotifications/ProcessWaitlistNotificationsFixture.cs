using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlist.ProcessWaitlistNotifications;

internal sealed class ProcessWaitlistNotificationsFixture
{
    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.New();
    public TimeZoneId TimeZone { get; } = TimeZoneId.From("UTC");

    private ProcessWaitlistNotificationsFixture()
    {
    }

    /// <summary>
    /// One waitlist entry, one freed slot — happy path.
    /// </summary>
    public static ProcessWaitlistNotificationsFixture WithOneEntryOneSlot() =>
        new();

    /// <summary>
    /// Two waitlist entries, only one freed slot.
    /// </summary>
    public static ProcessWaitlistNotificationsFixture WithTwoEntriesOneSlot() =>
        new();

    /// <summary>
    /// One waitlist entry, two freed slots — fewer entries than slots.
    /// </summary>
    public static ProcessWaitlistNotificationsFixture WithOneEntryTwoSlots() =>
        new();

    public async ValueTask SetupAsync(
        IntegrationTestEnvironment environment,
        int activeEntries = 1,
        int maxCapacity = 1)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            // TicketedEvent — needed for timezone + quiet hours
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                EventId,
                TeamId,
                EventName.From("DevConf 2026"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZone);
            dbContext.TicketedEvents.Add(ticketedEvent);

            // TicketCatalog — ticket type with WaitlistEnabled + WaitlistMode active
            var catalog = TicketCatalog.Create(EventId);
            catalog.AddTicketType(TicketTypeId, TicketTypeName.From("Conference Pass"), [], maxCapacity, waitlistEnabled: true, claimWindowHours: 8);

            // Fill to capacity and trigger WaitlistMode
            for (var i = 0; i < maxCapacity; i++)
                catalog.Claim([TicketTypeId], enforce: true);

            dbContext.TicketCatalogs.Add(catalog);

            // Waitlist with active entries
            var waitlist = global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist.Create(EventId, TicketTypeId, TeamId);
            var now = DateTimeOffset.UtcNow;
            for (var i = 0; i < activeEntries; i++)
                waitlist.AddEntry(EmailAddress.From($"attendee{i + 1}@example.com"), now.AddMinutes(i));

            dbContext.Waitlists.Add(waitlist);
        });
    }
}
