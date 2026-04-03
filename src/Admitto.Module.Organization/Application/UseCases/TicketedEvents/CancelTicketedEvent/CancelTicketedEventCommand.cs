using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CancelTicketedEvent;

internal sealed record CancelTicketedEventCommand(Guid TeamId, Guid EventId, uint? ExpectedVersion) : Command;
