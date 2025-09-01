namespace Amolenk.Admitto.Domain.ValueObjects;

public class TicketedEventPolicies
{
    public RegistrationPolicy? RegistrationPolicy { get; set; }

    public CancellationPolicy? CancellationPolicy { get; set; }
    
    // public ReconfirmationPolicy? ReconfirmationPolicy { get; set; }
    //
    // public ReminderPolicy? ReminderPolicy { get; set; }
    
}