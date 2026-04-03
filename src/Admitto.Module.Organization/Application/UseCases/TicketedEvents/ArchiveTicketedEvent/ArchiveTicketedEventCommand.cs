using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.ArchiveTicketedEvent;

internal sealed record ArchiveTicketedEventCommand(Guid TeamId, Guid EventId, uint? ExpectedVersion) : Command;
