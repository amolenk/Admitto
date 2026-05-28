using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.RemoveTeamMembership;

internal sealed record RemoveTeamMembershipCommand(Guid TeamId, string EmailAddress) : Command;
