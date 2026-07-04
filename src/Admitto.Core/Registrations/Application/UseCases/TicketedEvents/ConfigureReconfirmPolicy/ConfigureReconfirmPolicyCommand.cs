using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureReconfirmPolicy;

internal sealed record ConfigureReconfirmPolicyCommand(
    Guid EventId,
    Guid TeamId,
    uint? ExpectedVersion,
    DateTimeOffset? OpensAt,
    DateTimeOffset? ClosesAt,
    int? CadenceHours,
    int? MinEmailIntervalHours) : Command;
