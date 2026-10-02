using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.Composing;
using Amolenk.Admitto.Core.Registrations.Application.Messaging;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;

/// <summary>
/// Drives a registration's AttendeeRegistered or TicketsChanged domain event through the real integration
/// event publisher, ticket-confirmation email adapter, and composer, returning the prepared email delivery.
/// </summary>
internal static class TicketConfirmationEmailPipeline
{
    public static async ValueTask<PrepareEmailDeliveryCommand> PrepareAsync(
        IntegrationTestEnvironment environment,
        TeamId teamId,
        TicketedEventId eventId,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var outbox = Substitute.For<IOutbox>();
        IIntegrationEvent? integrationEvent = null;
        outbox.When(o => o.Enqueue(Arg.Any<IIntegrationEvent>()))
            .Do(ci => integrationEvent = ci.Arg<IIntegrationEvent>());
        var publisher = new RegistrationsIntegrationEventPublisher(outbox);
        switch (domainEvent)
        {
            case AttendeeRegisteredDomainEvent registered:
                await publisher.HandleAsync(registered, cancellationToken);
                break;
            case TicketsChangedDomainEvent changed:
                await publisher.HandleAsync(changed, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(domainEvent), domainEvent, "Not a ticket-confirmation cause.");
        }

        var composerFixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await composerFixture.SetupAsync(environment, teamId, eventId);
        var composer = composerFixture.BuildComposer(environment);
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        switch (integrationEvent)
        {
            case AttendeeRegisteredIntegrationEvent registered:
                await new AttendeeRegisteredIntegrationEventHandler(composer, deliveryHandler)
                    .HandleAsync(registered, cancellationToken);
                break;
            case AttendeeTicketsChangedIntegrationEvent changed:
                await new AttendeeTicketsChangedIntegrationEventHandler(composer, deliveryHandler)
                    .HandleAsync(changed, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unexpected integration event '{integrationEvent}'.");
        }

        return deliveryHandler.ReceivedDelivery();
    }
}
