using Amolenk.Admitto.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.RegisterExternalUser.EventHandlers;

internal class UserAddedDomainEventHandler(RegisterExternalUserHandler registerExternalUserHandler)
    : IDomainEventHandler<UserAddedDomainEvent>
{
    public async ValueTask HandleAsync(UserAddedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new RegisterExternalUserCommand(domainEvent.EmailAddress)
        {
            CommandId = CommandId.For<RegisterExternalUserCommand>(domainEvent.EventId)
        };

        await registerExternalUserHandler.HandleAsync(command, cancellationToken);
    }
}