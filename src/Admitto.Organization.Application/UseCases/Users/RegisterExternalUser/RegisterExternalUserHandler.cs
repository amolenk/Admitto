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
        var userId = UserId.From(command.UserId);
        var user = await writeStore.Users.GetAsync(userId, cancellationToken);

        if (user.ExternalUserId is null)
        {
            var externalUserId = await userDirectory.UpsertUserAsync(user.EmailAddress.Value, cancellationToken);

            user.AssignExternalUserId(ExternalUserId.From(externalUserId));
        }
    }
}