namespace Amolenk.Admitto.Application.UseCases.Auth.RegisterTicketedEvent;

public record RegisterTicketedEventCommand(Guid TeamId, string TicketedEventSlug) : Command;
