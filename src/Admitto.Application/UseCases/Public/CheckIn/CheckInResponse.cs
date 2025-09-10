namespace Amolenk.Admitto.Application.UseCases.Public.CheckIn;

public record CheckInResponse(
    string Email,
    string FirstName,
    string LastName,
    string? AttendeeStatus,
    string? ContributorStatus,
    DateTimeOffset LastModifiedAt);