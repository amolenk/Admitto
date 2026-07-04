using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ResolvePartnerRegistration;

internal sealed class ResolvePartnerRegistrationHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<ResolvePartnerRegistrationQuery, PartnerRegistrationResolutionDto?>
{
    public async ValueTask<PartnerRegistrationResolutionDto?> HandleAsync(
        ResolvePartnerRegistrationQuery query,
        CancellationToken cancellationToken)
    {
        var email = EmailAddress.From(query.Email);
        var teamId = TeamId.From(query.TeamId);

        var registration = await writeStore.Registrations
            .AsNoTracking()
            .Where(r => r.TeamId == teamId && r.EventId == query.EventId && r.Email == email &&
                        r.Status == RegistrationStatus.Registered)
            .Select(r => new { r.Id })
            .FirstOrDefaultAsync(cancellationToken);

        return registration is null
            ? null
            : new PartnerRegistrationResolutionDto(registration.Id.Value);
    }
}
