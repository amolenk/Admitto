using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.RegisterExternalUser.EventHandlers;

internal sealed class UserCreatedDomainEventHandler(
    [FromKeyedServices(OrganizationModule.Key)] IOutbox outbox)
    : IDomainEventHandler<UserCreatedDomainEvent>
{
    public ValueTask HandleAsync(UserCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new RegisterExternalUserCommand(domainEvent.UserId.Value)
        {
            CommandId = DeterministicCommandId<RegisterExternalUserCommand>.Create(domainEvent.EventId.Value)
        });

        return ValueTask.CompletedTask;
    }
}
