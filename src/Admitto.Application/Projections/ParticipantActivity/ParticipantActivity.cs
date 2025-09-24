namespace Amolenk.Admitto.Application.Projections.ParticipantActivity;

public enum ParticipantActivity
{
    // Registration lifecycle
    Registered,
    TicketSelectionChanged,
    CanceledOnTime,
    CanceledLate,
    Reconfirmed,
    
    // Communication
    EmailSent,
    
    // Contributor roles
    CrewAdded,
    SpeakerAdded,
    SponsorAdded,
    CrewRemoved,
    SpeakerRemoved,
    SponsorRemoved
}