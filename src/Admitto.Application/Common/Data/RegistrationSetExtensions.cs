using Amolenk.Admitto.Application.Common.Validation;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Data;

public static class RegistrationSetExtensions
{
    public static async ValueTask<Registration> GetRegistrationAsync(
        this DbSet<Registration> registrations,
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        var registration = await registrations.FindAsync([registrationId], cancellationToken);
        if (registration is null)
        {
            throw ValidationError.AttendeeRegistration.NotFound(registrationId);
        }

        return registration;
    }
}