using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamMembershipManagement.RemoveTeamMembership;

internal sealed record RemoveTeamMembershipCommand(Guid TeamId, string EmailAddress) : Command;
