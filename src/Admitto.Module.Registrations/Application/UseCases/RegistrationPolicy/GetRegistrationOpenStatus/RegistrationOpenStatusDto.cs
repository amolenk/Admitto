namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.GetRegistrationOpenStatus;

public sealed record RegistrationOpenStatusDto(
    bool IsOpen,
    bool IsEventActive,
    DateTimeOffset? WindowOpensAt,
    DateTimeOffset? WindowClosesAt);
