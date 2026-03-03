using Amolenk.Admitto.Organization.Application.Mapping;
using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.AssignTeamMembership;

internal sealed class AssignTeamMembershipHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<AssignTeamMembershipCommand>
{
    public async ValueTask HandleAsync(AssignTeamMembershipCommand command, CancellationToken cancellationToken)
    {
        var emailAddress = EmailAddress.From(command.EmailAddress);
        
        var user = await writeStore.Users
            .FirstOrDefaultAsync(u => u.EmailAddress == emailAddress, cancellationToken);

        if (user is null)
        {
            user = User.Create(emailAddress);
            writeStore.Users.Add(user);
        }

        user.AddTeamMembership(TeamId.From(command.TeamId), command.Role.ToDomain());
    }
}