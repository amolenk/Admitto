namespace Amolenk.Admitto.Application.UseCases.Attendees.RejectRegistration;

public record RejectRegistrationCommand(Guid AttendeeId) : Command;
