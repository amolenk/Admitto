using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.RemoveTeamMembership;

internal sealed record RemoveTeamMembershipCommand(Guid TeamId, string EmailAddress) : Command;
