using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ArchiveTicketedEvent;

internal sealed record ArchiveTicketedEventCommand(Guid EventId, Guid TeamId) : Command;
