namespace Amolenk.Admitto.Application.UseCases.Attendees.RejectRegistration;

public class RejectRegistrationHandler(IApplicationContext context) : ICommandHandler<RejectRegistrationCommand>
{
    public async ValueTask HandleAsync(RejectRegistrationCommand command, CancellationToken cancellationToken)
    {
        var attendee = await context.Attendees.GetEntityAsync(
            command.AttendeeId,
            cancellationToken: cancellationToken);
        
        attendee.FailRegistration();
    }
}