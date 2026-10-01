using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;

internal sealed class GetRegistrationDetailsHandler(
    IRegistrationsWriteStore writeStore,
    IRegistrationsReadStore readStore)
    : IQueryHandler<GetRegistrationDetailsQuery, RegistrationDetailDto?>
{
    public async ValueTask<RegistrationDetailDto?> HandleAsync(
        GetRegistrationDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var registration = await writeStore.Registrations
            .Where(r => r.Id == query.RegistrationId && r.EventId == query.EventId && r.TeamId == TeamId.From(query.TeamId))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (registration is null)
            return null;

        var activities = await readStore.ActivityLog
            .Where(a => a.RegistrationId == query.RegistrationId.Value
                        && a.EventId == query.EventId.Value
                        && a.TeamId == query.TeamId)
            .OrderBy(a => a.OccurredAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var waitlists = await writeStore.Waitlists
            .Where(w => w.EventId == query.EventId && w.TeamId == TeamId.From(query.TeamId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var catalog = await writeStore.TicketCatalogs
            .Where(c => c.Id == query.EventId && c.TeamId == TeamId.From(query.TeamId))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var waitlistEntries = waitlists
            .Select(w => (TicketType: catalog?.GetTicketType(w.Id), Position: w.GetActivePosition(registration.Email)))
            .Where(x => x.Position is not null && x.TicketType is not null)
            .Select(x => new WaitlistEntryDetailDto(x.TicketType!.Name.Value, x.Position!.Value))
            .ToList();

        return new RegistrationDetailDto(
            Id: registration.Id.Value,
            Email: registration.Email.Value,
            FirstName: registration.FirstName.Value,
            LastName: registration.LastName.Value,
            Status: registration.Status,
            RegisteredAt: registration.CreatedAt,
            HasReconfirmed: registration.HasReconfirmed,
            ReconfirmedAt: registration.ReconfirmedAt,
            CheckedInAt: registration.CheckedInAt,
            CancellationReason: registration.CancellationReason?.ToString(),
            Tickets: registration.Tickets
                .Select(t => new TicketDetailDto(t.Id.Value, t.Name.Value))
                .ToList(),
            WaitlistEntries: waitlistEntries,
            AdditionalDetails: registration.AdditionalDetails
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            Activities: activities
                .Select(a => new ActivityLogEntryDto(
                    ActivityType: a.ActivityType.ToString(),
                    OccurredAt: a.OccurredAt,
                    Metadata: a.Metadata))
                .ToList());
    }
}
