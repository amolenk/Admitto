using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventCreated;

internal sealed record HandleEventCreatedCommand(Guid TicketedEventId) : Command;
