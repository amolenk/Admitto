namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeams;

/// <summary>
/// Lightweight team summary returned by the list-teams endpoint.
/// </summary>
internal sealed record TeamListItemDto(string Slug, string Name, string EmailAddress, uint Version);
