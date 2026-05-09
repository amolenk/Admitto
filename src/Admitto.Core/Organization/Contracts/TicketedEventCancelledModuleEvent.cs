using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Contracts;

public sealed record TicketedEventCancelledModuleEvent(Guid TicketedEventId) : ModuleEvent;
