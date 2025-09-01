namespace Amolenk.Admitto.Application.UseCases.Participants.AdmitParticipant;

public record AdmitParticipantResponse(
    string Email,
    string FirstName,
    string LastName,
    string? AttendeeStatus,
    string? ContributorStatus,
    DateTimeOffset LastModifiedAt);