using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Application.Common.Identity;
using Amolenk.Admitto.Application.Projections.Admission;
using Amolenk.Admitto.Application.Projections.ParticipantHistory;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IApplicationContext
{
    DbSet<AdmissionView> AdmissionView { get; set; }
    DbSet<ParticipantHistoryView> AttendeeActivityView { get; }
    DbSet<Attendee> Attendees { get; }
    DbSet<Contributor> Contributors { get; }
    DbSet<EmailLog> EmailLog { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<EmailVerificationRequest> EmailVerificationRequests { get; }
    DbSet<Job> Jobs { get; }
    DbSet<Participant> Participants { get; set; }
    DbSet<ScheduledJob> ScheduledJobs { get; }
    DbSet<Team> Teams { get; }
    DbSet<TicketedEvent> TicketedEvents { get; }
    DbSet<TicketedEventAvailability> TicketedEventAvailability { get; }
}