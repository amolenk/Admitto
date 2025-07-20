namespace Amolenk.Admitto.Application.UseCases.Teams.GetTeam;

public record GetTeamResponse(string Slug, string Name, EmailSettingsDto EmailSettings);

public record EmailSettingsDto(string SenderEmail, string SmtpServer, int SmtpPort);


