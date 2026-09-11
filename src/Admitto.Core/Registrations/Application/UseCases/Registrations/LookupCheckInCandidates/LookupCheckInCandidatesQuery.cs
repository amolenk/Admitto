using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.LookupCheckInCandidates;

internal sealed record LookupCheckInCandidatesQuery(
    Guid TeamId,
    Guid EventId,
    string Query) : Query<IReadOnlyList<CheckInLookupCandidateDto>>;
