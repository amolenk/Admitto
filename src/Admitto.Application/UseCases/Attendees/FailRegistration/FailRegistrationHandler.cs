namespace Amolenk.Admitto.Application.UseCases.Attendees.FailRegistration;

public class FailRegistrationHandler(IApplicationContext context) : ICommandHandler<FailRegistrationCommand>
{
    public async ValueTask HandleAsync(FailRegistrationCommand command, CancellationToken cancellationToken)
    {
        var attendee = await context.Attendees.GetEntityAsync(
            command.AttendeeId,
            cancellationToken: cancellationToken);
        
        attendee.FailRegistration();
    }
}