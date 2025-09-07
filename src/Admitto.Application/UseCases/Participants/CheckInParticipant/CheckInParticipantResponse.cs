namespace Amolenk.Admitto.Application.UseCases.Participants.CheckInParticipant;

public record CheckInParticipantResponse(
    string Email,
    string FirstName,
    string LastName,
    string? AttendeeStatus,
    string? ContributorStatus,
    DateTimeOffset LastModifiedAt);