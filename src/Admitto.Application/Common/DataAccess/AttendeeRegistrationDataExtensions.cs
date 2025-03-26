using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.DataAccess;

public static class AttendeeRegistrationDataExtensions
{
    public static async ValueTask<AttendeeRegistration> GetByIdAsync(this DbSet<AttendeeRegistration> registrations, 
        Guid id, CancellationToken cancellationToken)
    {
        var attendeeRegistration = await registrations.FindAsync([id], cancellationToken);
        if (attendeeRegistration is null)
        {
            throw new AttendeeRegistrationNotFoundException(id);
        }
        
        return attendeeRegistration;
    }
}