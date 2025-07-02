using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.RequestConfirmation;

public class RequestUserConfirmationHandler(IDomainContext domainContext, IEmailOutbox emailOutbox)
    : ICommandHandler<RequestUserConfirmationCommand>
{
    public async ValueTask HandleAsync(RequestUserConfirmationCommand command, CancellationToken cancellationToken)
    {
        var registration = await domainContext.AttendeeRegistrations.FindAsync([command.RegistrationId], cancellationToken);
        if (registration is null)
        {
            throw ValidationError.AttendeeRegistration.NotFound(command.RegistrationId);
        }
        
        var emailParameters = GetEmailParameters(registration);
        
        await emailOutbox.EnqueueEmailAsync(registration.Email, EmailTemplateId.ConfirmRegistration, emailParameters,
            command.TicketedEventId, true, cancellationToken);
    }
    
    private static Dictionary<string, string> GetEmailParameters(AttendeeRegistration registration)
    {
        return registration.Details.ToDictionary(
                x => $"attendee_detail_{x.Name}", x => x.Value)
            .Concat(new Dictionary<string, string>
            {
                ["attendee_first_name"] = registration.FirstName,
                ["attendee_last_name"] = registration.LastName
            })
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}