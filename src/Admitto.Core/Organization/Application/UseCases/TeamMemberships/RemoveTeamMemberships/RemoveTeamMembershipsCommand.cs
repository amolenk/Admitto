using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.RemoveTeamMemberships;

internal sealed record RemoveTeamMembershipsCommand(Guid TeamId) : Command;
