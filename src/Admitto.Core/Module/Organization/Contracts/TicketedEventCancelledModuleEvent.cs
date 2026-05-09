using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Contracts;

public sealed record TicketedEventCancelledModuleEvent(Guid TicketedEventId) : ModuleEvent;
