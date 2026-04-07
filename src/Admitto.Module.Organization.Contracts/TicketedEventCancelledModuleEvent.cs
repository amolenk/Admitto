using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Contracts;

public sealed record TicketedEventCancelledModuleEvent(Guid TicketedEventId) : ModuleEvent;
