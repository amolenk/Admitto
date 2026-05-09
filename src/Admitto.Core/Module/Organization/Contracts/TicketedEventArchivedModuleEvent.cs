using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Contracts;

public sealed record TicketedEventArchivedModuleEvent(Guid TicketedEventId) : ModuleEvent;
