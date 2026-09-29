using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetPartnerRegistrationDetails;

internal sealed class GetPartnerRegistrationDetailsHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetPartnerRegistrationDetailsQuery, PartnerRegistrationDetailDto?>
{
    public async ValueTask<PartnerRegistrationDetailDto?> HandleAsync(
        GetPartnerRegistrationDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var registrationId = RegistrationId.From(query.RegistrationId);

        var registration = await writeStore.Registrations
            .Where(r => r.Id == registrationId && r.EventId == query.EventId && r.TeamId == TeamId.From(query.TeamId))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (registration is null)
            return null;

        var waitlists = await writeStore.Waitlists
            .Where(w => w.EventId == query.EventId && w.TeamId == TeamId.From(query.TeamId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var waitlistedTicketTypes = waitlists
            .Select(w => (TicketTypeId: w.Id.Value, Position: w.GetActivePosition(registration.Email)))
            .Where(x => x.Position is not null)
            .Select(x => new PartnerWaitlistedTicketTypeDto(x.TicketTypeId, x.Position!.Value))
            .ToList();

        return new PartnerRegistrationDetailDto(
            Id: registration.Id.Value,
            Email: registration.Email.Value,
            FirstName: registration.FirstName.Value,
            LastName: registration.LastName.Value,
            Status: registration.Status,
            TicketTypeIds: registration.Tickets
                .Select(t => t.Id.Value)
                .ToList(),
            Tickets: registration.Tickets
                .Select(t => new PartnerTicketDetailDto(t.Id.Value, t.Name.Value))
                .ToList(),
            WaitlistedTicketTypes: waitlistedTicketTypes,
            AdditionalDetails: registration.AdditionalDetails
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }
}
