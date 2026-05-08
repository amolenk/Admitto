using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy;

internal sealed record ConfigureReconfirmPolicyCommand(
    Guid EventId,
    uint? ExpectedVersion,
    DateTimeOffset? OpensAt,
    DateTimeOffset? ClosesAt,
    int? CadenceDays) : Command;
