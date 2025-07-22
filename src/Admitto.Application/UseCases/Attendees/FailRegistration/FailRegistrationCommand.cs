namespace Amolenk.Admitto.Application.UseCases.Attendees.FailRegistration;

public record FailRegistrationCommand(Guid AttendeeId) : Command;
