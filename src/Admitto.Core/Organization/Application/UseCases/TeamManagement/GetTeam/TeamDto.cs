namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.GetTeam;

internal sealed record TeamDto(Guid TeamId, string Name, string EmailAddress, uint Version);