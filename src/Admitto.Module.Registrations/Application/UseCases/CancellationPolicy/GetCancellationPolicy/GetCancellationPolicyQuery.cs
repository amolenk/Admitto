using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.GetCancellationPolicy;

internal sealed record GetCancellationPolicyQuery(TicketedEventId EventId)
    : Query<CancellationPolicyDto?>;
