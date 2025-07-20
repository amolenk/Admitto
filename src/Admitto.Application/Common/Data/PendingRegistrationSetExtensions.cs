using Amolenk.Admitto.Application.Common.Validation;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Data;

public static class PendingRegistrationSetExtensions
{
    public static async ValueTask<PendingRegistration> GetPendingRegistrationAsync(
        this IQueryable<PendingRegistration> pendingRegistrations,
        Guid pendingRegistrationId,
        bool noTracking = false,
        CancellationToken cancellationToken = default)
    {
        if (noTracking)
        {
            pendingRegistrations = pendingRegistrations.AsNoTracking();
        }
        
        var pendingRegistration = await pendingRegistrations
            .FirstOrDefaultAsync(r => r.Id == pendingRegistrationId, cancellationToken);

        if (pendingRegistration is null)
        {
            // TODO
            throw ValidationError.AttendeeRegistration.NotFound(pendingRegistrationId);
        }

        return pendingRegistration;
    }
}