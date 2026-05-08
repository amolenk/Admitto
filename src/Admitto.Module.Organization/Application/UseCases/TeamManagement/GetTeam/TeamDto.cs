namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeam;

internal sealed record TeamDto(Guid TeamId, string Name, string EmailAddress, uint Version);