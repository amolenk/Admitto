namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.UpdateTeam.AdminApi;

public sealed record UpdateTeamHttpRequest(
    string? Name,
    uint? ExpectedVersion)
{
    internal UpdateTeamCommand ToCommand(Guid teamId)
        => new(
            teamId,
            Name,
            ExpectedVersion);
}