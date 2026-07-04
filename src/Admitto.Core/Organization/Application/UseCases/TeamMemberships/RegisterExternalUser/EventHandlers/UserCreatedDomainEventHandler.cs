using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.RegisterExternalUser.EventHandlers;

internal sealed class UserCreatedDomainEventHandler(
    [FromKeyedServices(OrganizationModule.Key)] IOutbox outbox)
    : IDomainEventHandler<UserCreatedDomainEvent>
{
    public ValueTask HandleAsync(UserCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new RegisterExternalUserCommand(domainEvent.UserId.Value));

        return ValueTask.CompletedTask;
    }
}
