using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Capacity.InitializeTicketCapacity;

internal sealed record InitializeTicketCapacityCommand(Guid TicketedEventId, string Slug, int? Capacity) : Command;
