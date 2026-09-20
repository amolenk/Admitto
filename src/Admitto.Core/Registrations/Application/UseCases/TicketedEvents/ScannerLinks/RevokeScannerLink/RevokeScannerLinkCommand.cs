using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.RevokeScannerLink;

internal sealed record RevokeScannerLinkCommand(Guid TeamId, Guid EventId) : Command<ScannerLinkDto>;
