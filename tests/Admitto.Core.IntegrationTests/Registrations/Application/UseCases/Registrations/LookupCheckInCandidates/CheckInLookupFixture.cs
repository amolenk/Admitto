using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.LookupCheckInCandidates;

internal sealed class CheckInLookupFixture
{
    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketedEventId OtherEventId { get; } = TicketedEventId.New();
    public RegistrationId ExactRegistrationId { get; private set; } = RegistrationId.New();

    private CheckInLookupFixture() { }

    public static CheckInLookupFixture Candidates() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var ticketTypeId = TicketTypeId.New();
        var eventEntity = CreateEvent(EventId, "Lookup Event");
        var otherEventEntity = CreateEvent(OtherEventId, "Other Event");
        var registrations = new List<Registration>();

        for (var i = 0; i < 25; i++)
        {
            var registration = Registration.Create(
                TeamId,
                EventId,
                EmailAddress.From($"match{i}@example.com"),
                FirstName.From("Match"),
                LastName.From($"{i:00}"),
                [new TicketTypeSnapshot(ticketTypeId, TicketTypeName.From("General"), [])]);

            if (i == 0)
            {
                registration.CheckIn(DateTimeOffset.UtcNow);
                ExactRegistrationId = registration.Id;
            }
            else if (i == 1)
            {
                registration.Cancel(CancellationReason.AttendeeRequest);
            }

            registrations.Add(registration);
        }

        registrations.Add(Registration.Create(
            TeamId,
            OtherEventId,
            EmailAddress.From("match-other@example.com"),
            FirstName.From("Match"),
            LastName.From("Other"),
            [new TicketTypeSnapshot(ticketTypeId, TicketTypeName.From("General"), [])]));

        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.AddRange(eventEntity, otherEventEntity);
            db.Registrations.AddRange(registrations);
        });
    }

    private TicketedEvent CreateEvent(TicketedEventId id, string name) =>
        TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            id,
            TeamId,
            EventName.From(name),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(31),
            TimeZoneId.From("UTC"));
}
