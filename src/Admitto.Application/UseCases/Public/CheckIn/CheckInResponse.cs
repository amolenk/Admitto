using Amolenk.Admitto.Application.Projections.Participation;

namespace Amolenk.Admitto.Application.UseCases.Public.CheckIn;

public record CheckInResponse(
    string Email,
    string FirstName,
    string LastName,
    ParticipationAttendeeStatus? AttendeeStatus,
    ParticipationContributorStatus? ContributorStatus,
    DateTimeOffset LastModifiedAt);