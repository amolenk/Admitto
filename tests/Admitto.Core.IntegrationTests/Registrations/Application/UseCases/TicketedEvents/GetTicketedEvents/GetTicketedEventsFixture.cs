using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.GetTicketedEvents;

internal sealed class GetTicketedEventsFixture
{
    public TeamId TeamId { get; } = TeamId.New();

    private GetTicketedEventsFixture() { }

    public static GetTicketedEventsFixture WithMixedStatuses() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var active = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                TicketedEventId.New(),
                TeamId,
                EventName.From("Conf 2026"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(32),
                TimeZoneId.From("UTC"));

            var active2 = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                TicketedEventId.New(),
                TeamId,
                EventName.From("Meetup Q1"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(10),
                DateTimeOffset.UtcNow.AddDays(11),
                TimeZoneId.From("UTC"));

            var archived = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                TicketedEventId.New(),
                TeamId,
                EventName.From("Conf 2025"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(-60),
                DateTimeOffset.UtcNow.AddDays(-58),
                TimeZoneId.From("UTC"));
            archived.Archive();

            dbContext.TicketedEvents.AddRange(active, active2, archived);
        });
    }
}
