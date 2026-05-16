using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy;

internal sealed record ConfigureReconfirmPolicyCommand(
    Guid EventId,
    uint? ExpectedVersion,
    DateTimeOffset? OpensAt,
    DateTimeOffset? ClosesAt,
    int? CadenceHours,
    int? MinEmailIntervalHours) : Command;
