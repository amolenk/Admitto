namespace Amolenk.Admitto.Module.Shared.Contracts;

public sealed record UserContextDto(
    Guid UserId,
    string UserName,
    string EmailAddress);