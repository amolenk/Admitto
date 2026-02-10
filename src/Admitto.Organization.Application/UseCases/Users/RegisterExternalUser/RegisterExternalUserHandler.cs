using Amolenk.Admitto.Organization.Application.Services;
using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.RegisterExternalUser;

internal class RegisterExternalUserHandler(IExternalUserDirectory userDirectory)
    : ICommandHandler<RegisterExternalUserCommand>
{
    public async ValueTask HandleAsync(RegisterExternalUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userDirectory.GetUserByEmailAsync(command.EmailAddress, cancellationToken);
        if (user is not null) return;
                   
        await userDirectory.AddUserAsync(command.EmailAddress, cancellationToken);
    }
}