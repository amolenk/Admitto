using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;

internal sealed class GetRegistrationDetailsHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetRegistrationDetailsQuery, RegistrationDetailDto?>
{
    public async ValueTask<RegistrationDetailDto?> HandleAsync(
        GetRegistrationDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var registration = await writeStore.Registrations
            .Where(r => r.Id == query.RegistrationId && r.EventId == query.EventId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (registration is null)
            return null;

        var activities = await writeStore.ActivityLog
            .Where(a => a.RegistrationId == query.RegistrationId.Value)
            .OrderBy(a => a.OccurredAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new RegistrationDetailDto(
            Id: registration.Id.Value,
            Email: registration.Email.Value,
            FirstName: registration.FirstName.Value,
            LastName: registration.LastName.Value,
            Status: registration.Status,
            RegisteredAt: registration.CreatedAt,
            HasReconfirmed: registration.HasReconfirmed,
            ReconfirmedAt: registration.ReconfirmedAt,
            CancellationReason: registration.CancellationReason?.ToString(),
            Tickets: registration.Tickets
                .Select(t => new TicketDetailDto(t.Slug, t.Name))
                .ToList(),
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
