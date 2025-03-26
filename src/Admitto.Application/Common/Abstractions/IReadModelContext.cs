using Amolenk.Admitto.Application.ReadModel.Views;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IReadModelContext
{
    DbSet<AttendeeActivityView> AttendeeActivities { get; }

    DbSet<TeamMembersView> TeamMembers { get; }
}