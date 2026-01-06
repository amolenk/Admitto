using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Application.Common.Email.Verification;
using Amolenk.Admitto.Application.Projections.ParticipantActivity;
using Amolenk.Admitto.Application.Projections.Participation;
using Amolenk.Admitto.Application.Projections.TeamMember;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Persistence;

public interface IApplicationContext
{
    DbSet<Attendee> Attendees { get; }
    DbSet<BulkEmailWorkItem> BulkEmailWorkItems { get; }
    DbSet<Contributor> Contributors { get; }
    DbSet<EmailLog> EmailLog { get; }
    DbSet<EmailRecipientList> EmailRecipientLists { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<EmailVerificationRequest> EmailVerificationRequests { get; }
    DbSet<Participant> Participants { get; set; }
    DbSet<ParticipantActivityView> ParticipantActivityView { get; }
    DbSet<ParticipationView> ParticipationView { get; set; }
    DbSet<TeamMemberView> TeamMemberView { get; }
    DbSet<Team> Teams { get; }
    DbSet<TicketedEvent> TicketedEvents { get; }
}