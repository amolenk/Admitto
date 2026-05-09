using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ArchiveTicketedEvent;

internal sealed record ArchiveTicketedEventCommand(Guid EventId) : Command;
