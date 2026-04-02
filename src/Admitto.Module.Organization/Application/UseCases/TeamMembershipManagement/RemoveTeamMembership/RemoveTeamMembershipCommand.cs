using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.RemoveTeamMembership;

internal sealed record RemoveTeamMembershipCommand(Guid TeamId, string EmailAddress) : Command;
