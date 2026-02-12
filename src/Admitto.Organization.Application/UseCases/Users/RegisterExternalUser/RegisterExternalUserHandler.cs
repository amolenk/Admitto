using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Organization.Application.Services;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Application.Persistence;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.RegisterExternalUser;

internal sealed class RegisterExternalUserHandler(
    IOrganizationWriteStore writeStore,
    IExternalUserDirectory userDirectory)
    : ICommandHandler<RegisterExternalUserCommand>
{
    public async ValueTask HandleAsync(RegisterExternalUserCommand command, CancellationToken cancellationToken)
    {
        var user = await writeStore.Users.GetAsync(command.UserId, cancellationToken);

        if (user.ExternalUserId is null)
        {
            var externalUserId = await userDirectory.UpsertUserAsync(command.EmailAddress.Value, cancellationToken);

            user.AssignExternalUserId(ExternalUserId.From(externalUserId));
        }
    }
}