using Amolenk.Admitto.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Shared.Application.Mapping;
using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.RegisterExternalUser.EventHandlers;

internal sealed class UserAddedDomainEventHandler(IMediator mediator) : IDomainEventHandler<UserCreatedDomainEvent>
{
    public ValueTask HandleAsync(UserCreatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        mediator.SendAsync(
            new RegisterExternalUserCommand(domainEvent.UserId, domainEvent.EmailAddress)
            {
                CommandId = domainEvent.EventId.ToDeterministicCommandId<RegisterExternalUserCommand>()
            },
            cancellationToken);
}