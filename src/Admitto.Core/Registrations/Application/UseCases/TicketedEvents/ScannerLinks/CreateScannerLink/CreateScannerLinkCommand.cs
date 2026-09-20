using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.CreateScannerLink;

internal sealed record CreateScannerLinkCommand(Guid TeamId, Guid EventId) : Command<ScannerLinkDto>;
