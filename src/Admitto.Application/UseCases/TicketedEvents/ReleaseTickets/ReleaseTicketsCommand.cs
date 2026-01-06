using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.ReleaseTickets;

/// <summary>
/// Represents a command to release previously claimed tickets from a ticketed event.
/// </summary>
public record ReleaseTicketsCommand(Guid TicketedEventId, IList<TicketSelection> Tickets) : Command;