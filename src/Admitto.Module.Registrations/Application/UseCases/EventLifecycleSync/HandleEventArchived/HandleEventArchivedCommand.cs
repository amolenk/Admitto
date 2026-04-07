using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventArchived;

internal sealed record HandleEventArchivedCommand(Guid TicketedEventId) : Command;
