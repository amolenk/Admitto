namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.UpdateTeam.AdminApi;

public sealed record UpdateTeamHttpRequest(
    uint ExpectedVersion,
    string? Slug,
    string? Name,
    string? EmailAddress)
{
    internal UpdateTeamCommand ToCommand(Guid teamId)
        => new(
            teamId,
            ExpectedVersion,
            Slug,
            Name,
            EmailAddress);
}