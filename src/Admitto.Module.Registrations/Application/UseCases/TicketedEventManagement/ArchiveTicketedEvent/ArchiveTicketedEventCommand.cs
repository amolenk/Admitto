using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ArchiveTicketedEvent;

internal sealed record ArchiveTicketedEventCommand(Guid EventId) : Command;
