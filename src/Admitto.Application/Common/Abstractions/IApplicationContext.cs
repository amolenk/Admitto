using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Application.Common.Identity;
using Amolenk.Admitto.Application.Projections.ParticipantHistory;
using Amolenk.Admitto.Application.Projections.Participation;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IApplicationContext
{
    DbSet<Attendee> Attendees { get; }
    DbSet<BulkEmailWorkItem> BulkEmailWorkItems { get; }
    DbSet<Contributor> Contributors { get; }
    DbSet<EmailLog> EmailLog { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<EmailVerificationRequest> EmailVerificationRequests { get; }
    DbSet<Participant> Participants { get; set; }
    DbSet<ParticipantHistoryView> AttendeeActivityView { get; }
    DbSet<ParticipationView> AdmissionView { get; set; }
    DbSet<Team> Teams { get; }
    DbSet<TicketedEvent> TicketedEvents { get; }
    DbSet<TicketedEventAvailability> TicketedEventAvailability { get; }
}