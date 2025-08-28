using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Application.Common.Identity;
using Amolenk.Admitto.Application.Projections.ParticipantActivity;
using Amolenk.Admitto.Application.Projections.Participation;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IApplicationContext
{
    DbSet<AttendeeRegistration> AttendeeRegistrations { get; }

    DbSet<ContributorRegistration> ContributorRegistrations { get; }
    
    DbSet<EmailLog> EmailLog { get; }
    
    DbSet<EmailTemplate> EmailTemplates { get; }

    DbSet<EmailVerificationRequest> EmailVerificationRequests { get; }
    
    DbSet<Job> Jobs { get; }

    DbSet<ParticipantActivityView> ParticipantActivityView { get; }
    
    DbSet<ParticipationView> ParticipationView { get; }
    
    DbSet<ScheduledJob> ScheduledJobs { get; }

    DbSet<Team> Teams { get; }

    DbSet<TicketedEvent> TicketedEvents { get; }
}