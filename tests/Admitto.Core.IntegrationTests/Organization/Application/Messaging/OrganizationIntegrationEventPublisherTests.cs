using Amolenk.Admitto.Core.Organization.Application.Messaging;
using Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.Messaging;

[TestClass]
public sealed class OrganizationIntegrationEventPublisherTests
{
    // Given a ticketed event creation was requested
    // When the TicketedEventCreationRequested domain event is handled
    // Then a matching TicketedEventCreationRequested integration event is enqueued
    [TestMethod]
    public async ValueTask TicketedEventCreationRequested_EnqueuesIntegrationEvent()
    {
        var teamId = TeamId.New();
        var creationRequestId = CreationRequestId.New();
        var startsAt = DateTimeOffset.UtcNow.AddDays(10);
        var endsAt = startsAt.AddDays(1);
        var publicSlug = Slug.From("my-conference");

        var domainEvent = new TicketedEventCreationRequestedDomainEvent(
            creationRequestId,
            teamId,
            EventName.From("My Conference"),
            AbsoluteUrl.From("https://conf.example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            startsAt,
            endsAt,
            TimeZoneId.From("UTC"),
            publicSlug);

        IIntegrationEvent? captured = null;
        var outbox = Substitute.For<IOutbox>();
        outbox.When(o => o.Enqueue(Arg.Any<IIntegrationEvent>())).Do(ci => captured = ci.Arg<IIntegrationEvent>());

        var publisher = new OrganizationIntegrationEventPublisher(outbox);
        await publisher.HandleAsync(domainEvent, CancellationToken.None);

        captured.ShouldNotBeNull();
        var evt = captured.ShouldBeOfType<TicketedEventCreationRequestedIntegrationEvent>();
        evt.CreationRequestId.ShouldBe(creationRequestId.Value);
        evt.TeamId.ShouldBe(teamId.Value);
        evt.Name.ShouldBe("My Conference");
        evt.StartsAt.ShouldBe(startsAt);
        evt.EndsAt.ShouldBe(endsAt);
        evt.TimeZone.ShouldBe("UTC");
        evt.PublicSlug.ShouldBe(publicSlug.Value);
    }
}
