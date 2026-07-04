namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.UpdateTeam.AdminApi;

public sealed record UpdateTeamHttpRequest(
    string? Name,
    string? AccentColor,
    string? ReplyToEmailAddress,
    bool? ClearReplyToEmailAddress,
    uint? ExpectedVersion)
{
    internal UpdateTeamCommand ToCommand(Guid teamId)
        => new(
            teamId,
            Name,
            ExpectedVersion,
            AccentColor,
            ReplyToEmailAddress,
            ClearReplyToEmailAddress == true);
}
