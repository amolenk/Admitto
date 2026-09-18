namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.LookupCheckInCandidates;

public enum CheckInCandidateState
{
    Eligible,
    CheckedIn,
    Cancelled
}

public sealed record CheckInLookupCandidateDto(
    Guid RegistrationId,
    string Name,
    string Email,
    CheckInCandidateState State,
    DateTimeOffset? CheckedInAt);
