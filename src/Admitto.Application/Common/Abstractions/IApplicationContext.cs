using Amolenk.Admitto.Application.Projections.Attendance;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IApplicationContext
{
    DbSet<Attendee> Attendees { get; }

    DbSet<CrewMember> CrewMembers { get; }

    DbSet<EmailTemplate> EmailTemplates { get; }

    DbSet<Job> Jobs { get; }
    
    DbSet<ScheduledJob> ScheduledJobs { get; }

    DbSet<Speaker> Speakers { get; }

    DbSet<Team> Teams { get; }

    DbSet<TicketedEvent> TicketedEvents { get; }
    
    DbSet<AttendanceView> AttendanceView { get; }
}