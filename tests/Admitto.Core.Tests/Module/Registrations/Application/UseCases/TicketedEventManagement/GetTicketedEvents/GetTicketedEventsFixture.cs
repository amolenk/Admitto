using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Tests.Application.UseCases.TicketedEventManagement.GetTicketedEvents;

internal sealed class GetTicketedEventsFixture
{
    public TeamId TeamId { get; } = TeamId.New();

    private GetTicketedEventsFixture() { }

    public static GetTicketedEventsFixture WithMixedStatuses() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.Database.SeedAsync(dbContext =>
        {
            var active = TicketedEvent.Create(
                Guid.NewGuid(),
                TicketedEventId.New(),
                TeamId,
                DisplayName.From("Conf 2026"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(32),
                TimeZoneId.From("UTC"));

            var cancelled = TicketedEvent.Create(
                Guid.NewGuid(),
                TicketedEventId.New(),
                TeamId,
                DisplayName.From("Meetup Q1"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(10),
                DateTimeOffset.UtcNow.AddDays(11),
                TimeZoneId.From("UTC"));
            cancelled.Cancel();

            var archived = TicketedEvent.Create(
                Guid.NewGuid(),
                TicketedEventId.New(),
                TeamId,
                DisplayName.From("Conf 2025"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(-60),
                DateTimeOffset.UtcNow.AddDays(-58),
                TimeZoneId.From("UTC"));
            archived.Archive();

            dbContext.TicketedEvents.AddRange(active, cancelled, archived);
        });
    }
}
