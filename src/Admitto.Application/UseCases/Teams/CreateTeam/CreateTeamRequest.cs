using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

public record CreateTeamRequest(string Slug, string Name, EmailSettingsDto EmailSettings);

public record EmailSettingsDto(string SenderEmail, string SmtpServer, int SmtpPort);


