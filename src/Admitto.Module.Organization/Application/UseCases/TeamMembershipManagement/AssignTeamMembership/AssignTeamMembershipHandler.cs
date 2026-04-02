using Amolenk.Admitto.Module.Organization.Application.Mapping;
using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership;

internal sealed class AssignTeamMembershipHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<AssignTeamMembershipCommand>
{
    public async ValueTask HandleAsync(AssignTeamMembershipCommand command, CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);

        var team = await writeStore.Teams
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team is null)
        {
            throw new BusinessRuleViolationException(Errors.TeamNotFound(teamId));
        }

        // EC-10: Reject if the team is archived.
        team.EnsureNotArchived();

        var emailAddress = EmailAddress.From(command.EmailAddress);

        var user = await writeStore.Users
            .FirstOrDefaultAsync(u => u.EmailAddress == emailAddress, cancellationToken);

        if (user is null)
        {
            user = User.Create(emailAddress);
            writeStore.Users.Add(user);
        }

        user.AddTeamMembership(teamId, command.Role.ToDomain());
    }

    internal static class Errors
    {
        public static Error TeamNotFound(TeamId teamId) =>
            new(
                "team.not_found",
                "No team with the specified ID exists.",
                Type: ErrorType.NotFound,
                Details: new Dictionary<string, object?> { ["teamId"] = teamId.Value });
    }
}