using System.Text.Json;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;
using Shouldly;

namespace Amolenk.Admitto.Core.Registrations.Tests.Application.UseCases.Registrations.WriteActivityLog;

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
        var commandHandler = Substitute.For<ICommandHandler<WriteActivityLogCommand>>();
        commandHandler
            .HandleAsync(Arg.Do<WriteActivityLogCommand>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var handler = new TicketsChangedDomainEventHandler(commandHandler);
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.RegistrationId.ShouldBe(registrationId.Value);
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
