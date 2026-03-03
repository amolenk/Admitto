using Amolenk.Admitto.Organization.Application.ModuleEvents;
using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.RegisterExternalUser.EventHandlers;

internal sealed class UserAddedModuleEventHandler(IMediator mediator)
    : IModuleEventHandler<UserCreatedModuleEvent>
{
    public ValueTask HandleAsync(UserCreatedModuleEvent moduleEvent, CancellationToken cancellationToken) =>
        mediator.SendAsync(
            new RegisterExternalUserCommand(moduleEvent.UserId)
            {
                CommandId = DeterministicCommandId<RegisterExternalUserCommand>.Create(moduleEvent.EventId)
            },
            cancellationToken);
}