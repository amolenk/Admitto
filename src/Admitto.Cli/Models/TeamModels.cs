namespace Amolenk.Admitto.Cli.Models;

public record CreateTeamRequest(string Name, EmailSettingsDto EmailSettings, IEnumerable<TeamMemberDto> Members);

public record TeamMemberDto(string Email, string Role);

public record EmailSettingsDto(string SenderEmail, string SmtpServer, int SmtpPort);

public record CreateTeamResponse(Guid Id);