namespace Amolenk.Admitto.Application.UseCases.Auth.RegisterTicketedEvent;

public record RegisterTicketedEventCommand(Guid TeamId, Guid TicketedEventId) : Command;
