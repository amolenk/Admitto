using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.MaterializeTicketedEvent;

internal sealed class MaterializeTicketedEventFixture
{
    private bool _seedExistingEventWithSlug;

    public TeamId TeamId { get; } = TeamId.New();
    public Guid CreationRequestId { get; } = Guid.NewGuid();
    public string Slug { get; } = "new-conference";

    private MaterializeTicketedEventFixture() { }

    public static MaterializeTicketedEventFixture NoExistingEvent() => new();
    public static MaterializeTicketedEventFixture WithConflictingSlug() => new() { _seedExistingEventWithSlug = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (!_seedExistingEventWithSlug) return;

        await environment.Database.SeedAsync(dbContext =>
        {
            var existing = TicketedEvent.Create(
                TicketedEventId.New(),
                TeamId,
                Amolenk.Admitto.Module.Shared.Kernel.ValueObjects.Slug.From("test-team"),
                Amolenk.Admitto.Module.Shared.Kernel.ValueObjects.Slug.From(Slug),
                DisplayName.From("Existing"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(2),
                TimeZoneId.From("UTC"));
            dbContext.TicketedEvents.Add(existing);
        });
    }
}
