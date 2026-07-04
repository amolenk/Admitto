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
            AdditionalDetails: registration.AdditionalDetails
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }
}
