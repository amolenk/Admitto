namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.GetTeam;

internal sealed record TeamDto(
    Guid TeamId,
    string Name,
    string AccentColor,
    string? ReplyToEmailAddress,
    uint Version);
