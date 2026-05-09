using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamManagement.ArchiveTeam;

/// <summary>
/// Command to archive a team (US-005).
/// </summary>
internal sealed record ArchiveTeamCommand(Guid TeamId, uint? ExpectedVersion) : Command;
