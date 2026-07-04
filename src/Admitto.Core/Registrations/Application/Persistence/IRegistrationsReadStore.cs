using Amolenk.Admitto.Core.Registrations.Application.Projections.ActivityLog;

namespace Amolenk.Admitto.Core.Registrations.Application.Persistence;

public interface IRegistrationsReadStore
{
    DbSet<ActivityLogView> ActivityLog { get; }
}
