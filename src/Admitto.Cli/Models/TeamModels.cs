namespace Amolenk.Admitto.Cli.Models;

// TODO: Consider using Microsoft Kiota to generate these models from the OpenAPI specification
// for better type safety and automatic updates when the API changes.
// See: https://github.com/microsoft/kiota

public record CreateTeamRequest(string Name, EmailSettingsDto EmailSettings, IEnumerable<TeamMemberDto> Members);

public record TeamMemberDto(string Email, string Role);

public record EmailSettingsDto(string SenderEmail, string SmtpServer, int SmtpPort);

public record CreateTeamResponse(Guid Id);