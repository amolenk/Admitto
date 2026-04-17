namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.UpdateTeam.AdminApi;

public sealed record UpdateTeamHttpRequest(
    string? Name,
    string? EmailAddress,
    uint? ExpectedVersion)
{
    internal UpdateTeamCommand ToCommand(Guid teamId)
        => new(
            teamId,
            Name,
            EmailAddress,
            ExpectedVersion);
}