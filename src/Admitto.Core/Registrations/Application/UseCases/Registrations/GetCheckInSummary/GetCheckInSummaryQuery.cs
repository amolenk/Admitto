using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetCheckInSummary;

internal sealed record GetCheckInSummaryQuery(Guid TeamId, Guid EventId)
    : Query<CheckInSummaryDto?>;
