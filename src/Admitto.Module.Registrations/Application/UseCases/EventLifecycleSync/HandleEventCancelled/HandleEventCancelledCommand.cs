using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventCancelled;

internal sealed record HandleEventCancelledCommand(Guid TicketedEventId) : Command;
