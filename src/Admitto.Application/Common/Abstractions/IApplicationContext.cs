using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Application.Common.Identity;
using Amolenk.Admitto.Application.Projections.Participation;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IApplicationContext
{
    DbSet<CrewAssignment> CrewAssignments { get; }

    DbSet<EmailTemplate> EmailTemplates { get; }

    DbSet<Job> Jobs { get; }
 
    DbSet<AttendeeRegistration> AttendeeRegistrations { get; }

    DbSet<ScheduledJob> ScheduledJobs { get; }

    DbSet<SpeakerEngagement> SpeakerEngagements { get; }

    DbSet<Team> Teams { get; }

    DbSet<TicketedEvent> TicketedEvents { get; }
    
    DbSet<ParticipationView> ParticipationView { get; }
    
    DbSet<EmailVerificationRequest> EmailVerificationRequests { get; }
    
    DbSet<EmailLog> EmailLog { get; }
}