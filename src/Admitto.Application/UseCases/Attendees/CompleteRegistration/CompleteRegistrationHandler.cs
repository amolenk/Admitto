namespace Amolenk.Admitto.Application.UseCases.Attendees.CompleteRegistration;

public class CompleteRegistrationHandler(IDomainContext context) : ICommandHandler<CompleteRegistrationCommand>
{
    public async ValueTask HandleAsync(CompleteRegistrationCommand command, CancellationToken cancellationToken)
    {
        var attendee = await context.Attendees.GetEntityAsync(
            command.AttendeeId,
            cancellationToken: cancellationToken);
        
        attendee.CompleteRegistration();
    }
}