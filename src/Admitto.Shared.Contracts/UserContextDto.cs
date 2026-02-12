namespace Amolenk.Admitto.Shared.Contracts;

public sealed record UserContextDto(
    Guid UserId,
    string UserName,
    string EmailAddress);