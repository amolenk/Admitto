namespace Amolenk.Admitto.Application.Common.Exceptions;

public class AttendeeRegistrationNotFoundException(Guid attendeeRegistrationId, Exception? innerException = null)
    : Exception($"Registration {attendeeRegistrationId} not found.", innerException);