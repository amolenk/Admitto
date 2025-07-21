namespace Amolenk.Admitto.Application.UseCases.Attendees.CompleteRegistration;

public record CompleteRegistrationCommand(Guid AttendeeId) : Command;
