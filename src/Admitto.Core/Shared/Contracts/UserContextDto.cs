namespace Amolenk.Admitto.Core.Shared.Contracts;

public sealed record UserContextDto(
    Guid UserId,
    string UserName,
    string EmailAddress,
    bool IsAdmin = false,
    IReadOnlyList<UserContextTeamMembershipDto>? TeamMemberships = null);