using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.LookupCheckInCandidates;

internal sealed class LookupCheckInCandidatesHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<LookupCheckInCandidatesQuery, IReadOnlyList<CheckInLookupCandidateDto>>
{
    public async ValueTask<IReadOnlyList<CheckInLookupCandidateDto>> HandleAsync(
        LookupCheckInCandidatesQuery query,
        CancellationToken cancellationToken)
    {
        var value = query.Query.Trim();
        var registrations = writeStore.Registrations
            .AsNoTracking()
            .Where(r => r.TeamId == TeamId.From(query.TeamId) && r.EventId == TicketedEventId.From(query.EventId));

        if (Guid.TryParse(value, out var registrationId))
        {
            registrations = registrations.Where(r => r.Id == RegistrationId.From(registrationId));
        }
        else
        {
            if (value.Length < 2)
                return [];

            registrations = registrations.Where(r =>
                EF.Functions.ILike(r.SearchText, $"%{value.ToLowerInvariant()}%"));
        }

        var results = await registrations
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .Take(20)
            .ToListAsync(cancellationToken);

        return results.Select(r => new CheckInLookupCandidateDto(
                r.Id.Value,
                $"{r.FirstName.Value} {r.LastName.Value}",
                r.Email.Value,
                r.Status == RegistrationStatus.Cancelled
                    ? CheckInCandidateState.Cancelled
                    : r.CheckedInAt is not null
                        ? CheckInCandidateState.CheckedIn
                        : CheckInCandidateState.Eligible,
                r.CheckedInAt))
            .ToList();
    }
}
