namespace Amolenk.Admitto.Application.UseCases.Auth.RegisterTicketedEvent;

public record RegisterTicketedEventCommand(Guid TeamId, string EventSlug) : Command
{
    public Guid Id { get; } = Guid.NewGuid();
}
