namespace Amolenk.Admitto.Domain.ValueObjects;

public class TicketedEventPolicies
{
    public RegistrationPolicy? RegistrationPolicy { get; set; }

    public CancellationPolicy? CancellationPolicy { get; set; }
    
    public ReconfirmPolicy? ReconfirmPolicy { get; set; }
    //
    // public ReminderPolicy? ReminderPolicy { get; set; }
    
}