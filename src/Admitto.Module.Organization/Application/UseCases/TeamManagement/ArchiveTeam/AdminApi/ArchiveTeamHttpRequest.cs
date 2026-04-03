namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.ArchiveTeam.AdminApi;

/// <summary>
/// HTTP request body for the archive-team endpoint.
/// </summary>
public sealed record ArchiveTeamHttpRequest(uint? ExpectedVersion)
{
    internal ArchiveTeamCommand ToCommand(Guid teamId) => new(teamId, ExpectedVersion);
}
