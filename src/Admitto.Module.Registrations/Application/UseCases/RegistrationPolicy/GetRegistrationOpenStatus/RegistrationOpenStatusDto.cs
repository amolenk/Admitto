using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.GetRegistrationOpenStatus;

public sealed record RegistrationOpenStatusDto(
    RegistrationStatus Status,
    bool CanOpen,
    string? Reason);
