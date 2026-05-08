using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.CancelTicketedEvent;

internal sealed record CancelTicketedEventCommand(Guid EventId) : Command;
