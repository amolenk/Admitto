using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

public record CreateTeamRequest(string Name, EmailSettingsDto EmailSettings, IEnumerable<TeamMemberDto> Members);

public record TeamMemberDto(string Email, string Role);

public record EmailSettingsDto(string SenderEmail, string SmtpServer, int SmtpPort)
{
    public static EmailSettingsDto FromEmailSettings(EmailSettings settings)
    {
        return new EmailSettingsDto(settings.SenderEmail, settings.SmtpServer, settings.SmtpPort);
    }
}

