using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail.EventHandlers;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.TriggerBulkEmailJob;
using Amolenk.Admitto.Core.Email.Domain.DomainEvents;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.BulkEmails.CreateBulkEmail.EventHandlers;

[TestClass]
public sealed class BulkEmailJobRequestedDomainEventHandlerTests
{
    // Given a bulk email job has been requested
    // When the BulkEmailJobRequested domain event is handled
    // Then a TriggerBulkEmailJob command is enqueued for that job
    [TestMethod]
    public async ValueTask BulkEmailJobRequested_EnqueuesTriggerCommand()
    {
        var bulkEmailJobId = BulkEmailJobId.New();
        var teamId = TeamId.New();
        var ticketedEventId = TicketedEventId.New();

        var domainEvent = new BulkEmailJobRequestedDomainEvent(bulkEmailJobId, teamId, ticketedEventId);

        ICommand? captured = null;
        var outbox = Substitute.For<IOutbox>();
        outbox.When(o => o.Enqueue(Arg.Any<ICommand>())).Do(ci => captured = ci.Arg<ICommand>());

        var handler = new BulkEmailJobRequestedDomainEventHandler(outbox);
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        captured.ShouldNotBeNull();
        var command = captured.ShouldBeOfType<TriggerBulkEmailJobCommand>();
        command.BulkEmailJobId.ShouldBe(bulkEmailJobId.Value);
    }
}
