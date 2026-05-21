using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Application.Services;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.RegisterExternalUser;

internal sealed class RegisterExternalUserHandler(
    IOrganizationWriteStore writeStore,
    IExternalUserDirectory userDirectory)
    : ICommandHandler<RegisterExternalUserCommand>, IWorkerOnly
{
    public async ValueTask HandleAsync(RegisterExternalUserCommand command, CancellationToken cancellationToken)
    {
        var userId = UserId.From(command.UserId);
        var user = await writeStore.Users.GetAsync(userId, cancellationToken);

        if (user.ExternalUserId is null)
        {
            var externalUserId = await userDirectory.InviteUserAsync(user.EmailAddress.Value, cancellationToken);

            user.AssignExternalUserId(ExternalUserId.From(externalUserId));
        }
    }
}
