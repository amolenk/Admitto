namespace Amolenk.Admitto.Domain.ValueObjects;

public enum RegistrationStatus
{
    PendingUserVerification = 0,
    PendingCompletion = 1,
    Completed = 2,
    Rejected = 3,
    Reconfirmed = 4,
    Canceled = 5
}