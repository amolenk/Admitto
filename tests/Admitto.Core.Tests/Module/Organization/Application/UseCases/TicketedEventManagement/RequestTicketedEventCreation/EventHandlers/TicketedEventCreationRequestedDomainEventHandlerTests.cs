using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation.EventHandlers;
using Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.Organization.Tests.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation.EventHandlers;

[TestClass]
public sealed class TicketedEventCreationRequestedDomainEventHandlerTests
{
    [TestMethod]
    public async ValueTask SC001_TicketedEventCreationRequested_EnqueuesIntegrationEvent()
    {
        var teamId = TeamId.New();
        var creationRequestId = CreationRequestId.New();
        var startsAt = DateTimeOffset.UtcNow.AddDays(10);
        var endsAt = startsAt.AddDays(1);

        var domainEvent = new TicketedEventCreationRequestedDomainEvent(
            creationRequestId,
            teamId,
            DisplayName.From("My Conference"),
            AbsoluteUrl.From("https://conf.example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            startsAt,
            endsAt,
            TimeZoneId.From("UTC"));

        IIntegrationEvent? captured = null;
        var outbox = Substitute.For<IOutbox>();
        outbox.When(o => o.Enqueue(Arg.Any<IIntegrationEvent>())).Do(ci => captured = ci.Arg<IIntegrationEvent>());

        var handler = new TicketedEventCreationRequestedDomainEventHandler(outbox);
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        captured.ShouldNotBeNull();
        var evt = captured.ShouldBeOfType<TicketedEventCreationRequestedIntegrationEvent>();
        evt.CreationRequestId.ShouldBe(creationRequestId.Value);
        evt.TeamId.ShouldBe(teamId.Value);
        evt.Name.ShouldBe("My Conference");
        evt.StartsAt.ShouldBe(startsAt);
        evt.EndsAt.ShouldBe(endsAt);
        evt.TimeZone.ShouldBe("UTC");
    }
}
