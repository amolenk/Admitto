using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Contracts;

public sealed record TicketedEventArchivedModuleEvent(Guid TicketedEventId) : ModuleEvent;
