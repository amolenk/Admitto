using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.ResendTeamMemberInvite;

internal sealed record ResendTeamMemberInviteCommand(
    Guid TeamId,
    string EmailAddress)
    : Command;
