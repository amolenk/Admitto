using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEventManagement.CancelTicketedEvent;

internal sealed record CancelTicketedEventCommand(Guid EventId) : Command;
