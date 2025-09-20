namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.SetRegistrationPolicy;

public record SetRegistrationPolicyRequest(
    TimeSpan OpensBeforeEvent,
    TimeSpan ClosesBeforeEvent,
    string? EmailDomainName);
