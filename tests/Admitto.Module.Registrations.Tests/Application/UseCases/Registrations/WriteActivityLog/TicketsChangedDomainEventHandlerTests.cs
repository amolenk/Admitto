using System.Text.Json;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.WriteActivityLog;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;
using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using NSubstitute;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.WriteActivityLog;

[TestClass]
public sealed class TicketsChangedDomainEventHandlerTests
{
    [TestMethod]
    public async ValueTask SC001_TicketsChanged_DispatchesWriteActivityLogWithCorrectMetadata()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var changedAt = DateTimeOffset.UtcNow;

        var domainEvent = new TicketsChangedDomainEvent(
            teamId, eventId, registrationId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Test"),
            OldTickets: [new TicketTypeSnapshot("early-bird", "Early Bird", [])],
            NewTickets: [new TicketTypeSnapshot("workshop", "Workshop", [])],
            ChangedAt: changedAt);

        WriteActivityLogCommand? captured = null;
        var mediator = Substitute.For<IMediator>();
        mediator.When(m => m.SendAsync(Arg.Any<WriteActivityLogCommand>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<WriteActivityLogCommand>());

        var handler = new TicketsChangedDomainEventHandler(mediator);
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.RegistrationId.ShouldBe(registrationId);
        captured.ActivityType.ShouldBe(ActivityType.TicketsChanged);
        captured.OccurredAt.ShouldBe(changedAt);

        // Metadata must be {"from":["early-bird"],"to":["workshop"]}
        using var doc = JsonDocument.Parse(captured.Metadata!);
        var from = doc.RootElement.GetProperty("from").EnumerateArray().Select(e => e.GetString()!).ToArray();
        var to = doc.RootElement.GetProperty("to").EnumerateArray().Select(e => e.GetString()!).ToArray();
        from.ShouldBe(["early-bird"]);
        to.ShouldBe(["workshop"]);
    }
}
