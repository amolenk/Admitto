using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Capacity.UpdateTicketCapacity;

internal sealed record UpdateTicketCapacityCommand(Guid TicketedEventId, string Slug, int? Capacity) : Command;
