using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.RegenerateScannerLink;

internal sealed record RegenerateScannerLinkCommand(Guid TeamId, Guid EventId) : Command<ScannerLinkDto>;
