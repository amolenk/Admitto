namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

public class SendRejectionEmailHandler : ICommandHandler<SendRejectionEmailCommand>
{
    public ValueTask HandleAsync(SendRejectionEmailCommand command, CancellationToken cancellationToken)
    {
        // var emailParameters = GetEmailParameters(registration);
        // emailParameters["confirmation_code"] = domainEvent.ConfirmationCode;
        //
        // await emailOutbox.EnqueueEmailAsync(registration.Email, EmailTemplateId.ConfirmRegistration, emailParameters,
        //     domainEvent.TicketedEventId, true, cancellationToken);
        
        
        throw new NotImplementedException();
    }
    
    // private static Dictionary<string, string> GetEmailParameters(AttendeeRegistration registration)
    // {
    //     return registration.Details.ToDictionary(
    //             x => $"attendee_detail_{x.Name}", x => x.Value)
    //         .Concat(new Dictionary<string, string>
    //         {
    //             ["attendee_first_name"] = registration.FirstName,
    //             ["attendee_last_name"] = registration.LastName
    //         })
    //         .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    // }
}