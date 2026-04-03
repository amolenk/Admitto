using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CancelTicketType;

internal sealed record CancelTicketTypeCommand(Guid TeamId, Guid EventId, string TicketTypeSlug, uint? ExpectedVersion) : Command;
