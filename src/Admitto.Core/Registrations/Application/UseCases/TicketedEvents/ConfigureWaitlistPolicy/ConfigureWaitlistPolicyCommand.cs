using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureWaitlistPolicy;

internal sealed record ConfigureWaitlistPolicyCommand(
    Guid EventId,
    Guid TeamId,
    uint? ExpectedVersion,
    TimeOnly QuietHoursStart,
    TimeOnly QuietHoursEnd) : Command;
