namespace Amolenk.Admitto.Module.Organization.Application.UseCases.Teams.GetTeam;

internal sealed record TeamDto(string Slug, string Name, string EmailAddress, uint Version);