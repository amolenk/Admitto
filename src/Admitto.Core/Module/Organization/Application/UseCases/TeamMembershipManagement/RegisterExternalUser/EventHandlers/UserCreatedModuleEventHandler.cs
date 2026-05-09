using Amolenk.Admitto.Core.Module.Organization.Application.ModuleEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamMembershipManagement.RegisterExternalUser.EventHandlers;

internal sealed class UserCreatedModuleEventHandler(IMediator mediator)
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